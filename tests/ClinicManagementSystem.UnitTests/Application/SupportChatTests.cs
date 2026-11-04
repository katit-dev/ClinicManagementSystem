using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs.Support;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.UnitTests.Application;

public class SupportChatTests
{
    [Theory]
    [InlineData("me",11,true)] [InlineData("me",12,false)]
    [InlineData("reception",21,true)] [InlineData("reception",22,false)]
    [InlineData("admin",31,false)]
    public void ContentAccess_RequiresOwnerOrCurrentAssignee(string side,int uid,bool expected)
        =>Assert.Equal(expected,SupportChatRules.CanRead(new(){PatientUserId=11,AssignedReceptionistUserId=21,Status=1},uid,side));
    [Theory]
    [InlineData(3,5,10,5)] [InlineData(5,3,10,5)] [InlineData(0,0,0,0)]
    public void ReadCursor_IsMonotonic(long current,long requested,long last,long expected)
        =>Assert.Equal(expected,SupportChatRules.AdvanceRead(current,requested,last));
    [Theory] [InlineData(-1)] [InlineData(11)]
    public void ReadCursor_RejectsNonexistentPosition(long requested)
        =>Assert.Throws<ArgumentOutOfRangeException>(()=>SupportChatRules.AdvanceRead(3,requested,10));
    [Fact]
    public void PatientMessage_DoesNotSerializeInternalIdentity()
    {
        var dto=SupportChatRules.PatientMessage(new(){Id=5,SenderSide=1,SenderUserId=902,SenderNameSnapshot="Internal receptionist",Content="Xin chào"});
        var json=JsonSerializer.Serialize(dto);
        Assert.Contains("Hỗ trợ phòng khám",dto.SenderLabel);Assert.DoesNotContain("SenderUserId",json);
        Assert.DoesNotContain("SenderNameSnapshot",json);Assert.DoesNotContain("Internal receptionist",json);
    }
    [Fact]
    public void AutomaticAcknowledgement_HasNoExtraPatientLabel()
        =>Assert.Equal("",SupportChatRules.PatientMessage(new(){SenderSide=2,MessageKind=1}).SenderLabel);
    [Fact]
    public void CursorQuery_RejectsBothDirections()
    {
        var q=new SupportCursorQuery {BeforeSequence=10,AfterSequence=3};var errors=new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(q,new(q),errors,true));
    }
    [Theory] [InlineData("waiting",1)] [InlineData("mine",1)] [InlineData("closed",1)]
    public void ReceptionInbox_NeverListsAnotherAssigneesPrivateConversation(string scope,int expected)
    {
        var rows=new[]{new SupportConversation{Id=1,Status=0},new(){Id=2,Status=1,AssignedReceptionistUserId=21},
            new(){Id=3,Status=1,AssignedReceptionistUserId=22},new(){Id=4,Status=2,AssignedReceptionistUserId=21}};
        var result=SupportChatRepository.Visible(rows.AsQueryable(),21,"reception",scope).ToList();
        Assert.Equal(expected,result.Count);Assert.DoesNotContain(result,x=>x.Id==3);
    }
    [Fact]
    public void SupportMapping_HasRetryAndAcknowledgementUniqueIndexes()
    {
        using var db=new ClinicManagementDbContext(new DbContextOptionsBuilder<ClinicManagementDbContext>()
            .UseSqlServer("Server=unused.invalid;Database=TranslationOnly;Integrated Security=true;TrustServerCertificate=true").Options);
        var model=db.Model.FindEntityType(typeof(SupportMessage))!;
        Assert.Contains(model.GetIndexes(),i=>i.IsUnique && i.GetFilter()=="[message_kind] = 1");
        Assert.Contains(model.GetIndexes(),i=>i.IsUnique && i.Properties.Select(p=>p.Name).SequenceEqual(new[]{"SenderUserId","ClientMessageId"}));
        var sql=SupportChatRepository.Visible(db.SupportConversations,11,"me","").OrderBy(c=>c.Id).Skip(20).Take(20).ToQueryString();
        Assert.Contains("patient_user_id",sql);Assert.Contains("OFFSET",sql);
    }
}
