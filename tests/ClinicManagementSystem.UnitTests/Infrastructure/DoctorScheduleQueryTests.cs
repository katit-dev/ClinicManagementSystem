using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.UnitTests.Infrastructure;

public class DoctorScheduleQueryTests
{
    [Theory]
    [InlineData(null,2)]
    [InlineData(0,1)]
    [InlineData(1,1)]
    public void TimeOffScope_IncludesOnlySelfAndClinic(int? scope,int expected)
    {
        var rows=new[]{Row(1,7),Row(2,99),Row(3,null)}.AsQueryable();
        var result=DoctorTimeOffRepository.FilterDoctorTimeOff(rows,7,null,null,scope.HasValue?(byte)scope.Value:null,null).ToList();
        Assert.Equal(expected,result.Count);Assert.DoesNotContain(result,r=>r.DoctorId==99);
    }
    [Fact]
    public void OverlapFilters_UseExclusiveEnd_AndStablePaging()
    {
        var rows=new[]{Row(1,7),Row(2,7),Row(3,7)};
        rows[0].EndAt=new(2026,10,20); rows[2].StartAt=new(2026,10,21);
        var filtered=DoctorTimeOffRepository.FilterDoctorTimeOff(rows.AsQueryable(),7,new(2026,10,20),new(2026,10,21),null,0).ToList();
        Assert.Equal(2,Assert.Single(filtered).Id);
        var ordered=DoctorTimeOffRepository.FilterDoctorTimeOff(new[]{Row(1,7),Row(2,7)}.AsQueryable(),7,null,null,null,null);
        Assert.Equal(2,Assert.Single(ordered.Take(1)).Id);Assert.Equal(1,Assert.Single(ordered.Skip(1).Take(1)).Id);
    }
    [Fact]
    public void Filters_TranslateToSqlServer_WithoutAConnection()
    {
        using var db=new ClinicManagementDbContext(new DbContextOptionsBuilder<ClinicManagementDbContext>().UseSqlServer("Server=unused.invalid;Database=TranslationOnly;Integrated Security=true;TrustServerCertificate=true").Options);
        var sql=DoctorTimeOffRepository.FilterDoctorTimeOff(db.DoctorTimeOffs,7,new(2026,10,1),new(2026,11,1),0,2).Skip(10).Take(10).ToQueryString();
        Assert.Contains("[doctor_id]",sql);Assert.Contains("[start_at]",sql);Assert.Contains("[end_at]",sql);Assert.Contains("OFFSET",sql);
    }
    private static DoctorTimeOff Row(int id,int? doctor)=>new(){Id=id,DoctorId=doctor,Type=0,StartAt=new(2026,10,20,8,0,0),EndAt=new(2026,10,20,17,0,0)};
}
