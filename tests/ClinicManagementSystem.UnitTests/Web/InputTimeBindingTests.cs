using System.Linq.Expressions;
using ClinicManagementSystem.Application.DTOs.Scheduling;
using ClinicManagementSystem.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.UnitTests.Web;

#pragma warning disable BL0006 // The test renderer dispatches actual browser event callbacks.
public class InputTimeBindingTests
{
    [Theory]
    [InlineData("StartTime", "17:00")]
    [InlineData("EndTime", "17:00")]
    [InlineData("BreakStart", "17:00")]
    [InlineData("BreakEnd", "17:00")]
    [InlineData("StartTime", "17:00:00")]
    [InlineData("StartTime", "17:00:00.000")]
    public async Task ChoosingHour_UpdatesModelImmediately_AndSurvivesNextFieldRender(string field, string text)
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new EventRenderer(services);
        var host = new TimeFormHost();
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.MountAsync(host);
            await renderer.InputAsync(field, text);
            Assert.Equal(new TimeOnly(17, 0), typeof(ScheduleRequestDTO).GetProperty(field)!.GetValue(host.Model));
            var next = field == "EndTime" ? "StartTime" : "EndTime";
            await renderer.InputAsync(next, "19:00");
            Assert.Equal("17:00", renderer.Value(field));
            Assert.Equal(new TimeOnly(17, 0), typeof(ScheduleRequestDTO).GetProperty(field)!.GetValue(host.Model));
        });
    }

    [Fact]
    public async Task ClearingBreak_SetsNull_AndNotifiesEditContext()
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new EventRenderer(services);
        var host = new TimeFormHost();
        var changed = new List<string>();
        host.Context.OnFieldChanged += (_, args) => changed.Add(args.FieldIdentifier.FieldName);
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.MountAsync(host);
            await renderer.InputAsync("BreakStart", "");
            Assert.Null(host.Model.BreakStart);
            Assert.Contains("BreakStart", changed);
            Assert.Equal(string.Empty, renderer.Value("BreakStart"));
        });
    }

    [Fact]
    public async Task InvalidHour_ShowsValidation_InsteadOfSilentlyResettingToDefault()
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new EventRenderer(services);
        var host = new TimeFormHost();
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.MountAsync(host);
            await renderer.InputAsync("StartTime", "25:00");
            Assert.Equal(new TimeOnly(8, 0), host.Model.StartTime);
            Assert.Equal("25:00", renderer.Value("StartTime"));
            Assert.Single(host.Context.GetValidationMessages(new FieldIdentifier(host.Model, "StartTime")));
            await renderer.InputAsync("StartTime", "17:00");
            Assert.Equal(new TimeOnly(17, 0), host.Model.StartTime);
            Assert.Empty(host.Context.GetValidationMessages(new FieldIdentifier(host.Model, "StartTime")));
        });
    }

    private sealed class TimeFormHost : ComponentBase
    {
        public ScheduleRequestDTO Model { get; } = new() { StartTime = new(8, 0), EndTime = new(8, 0), BreakStart = new(8, 0), BreakEnd = new(8, 0) };
        public EditContext Context { get; }
        public TimeFormHost() => Context = new(Model);
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<EditForm>(0);
            builder.AddAttribute(1, "EditContext", Context);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<EditContext>)(_ => children =>
            {
                AddTime(children, "StartTime", Model.StartTime, v => Model.StartTime = v, () => Model.StartTime);
                AddTime(children, "EndTime", Model.EndTime, v => Model.EndTime = v, () => Model.EndTime);
                AddTime(children, "BreakStart", Model.BreakStart, v => Model.BreakStart = v, () => Model.BreakStart);
                AddTime(children, "BreakEnd", Model.BreakEnd, v => Model.BreakEnd = v, () => Model.BreakEnd);
            }));
            builder.CloseComponent();
        }
        private void AddTime(RenderTreeBuilder builder, string id, TimeOnly? value, Action<TimeOnly?> set, Expression<Func<TimeOnly?>> expression)
        {
            builder.OpenComponent<InputTime>(0);
            builder.SetKey(id);
            builder.AddAttribute(1, "Value", value);
            builder.AddAttribute(2, "ValueChanged", EventCallback.Factory.Create(this, set));
            builder.AddAttribute(3, "ValueExpression", expression);
            builder.AddAttribute(4, "AdditionalAttributes", new Dictionary<string, object> { ["id"] = id });
            builder.CloseComponent();
        }
    }

    private sealed class EventRenderer : Renderer
    {
        private readonly HashSet<int> _ids = new();
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        public EventRenderer(IServiceProvider services) : base(services, services.GetRequiredService<ILoggerFactory>()) { }
        public Task MountAsync(IComponent component) => RenderRootComponentAsync(AssignRootComponentId(component));
        public Task InputAsync(string id, string value)
            => DispatchEventAsync(Attributes(id).Single(f => f.AttributeName == "oninput").AttributeEventHandlerId, null, new ChangeEventArgs { Value = value });
        public string? Value(string id)
        {
            foreach (var attribute in Attributes(id))
                if (attribute.AttributeName == "value") return attribute.AttributeValue?.ToString();
            return string.Empty; // A null HTML value is rendered as an omitted attribute.
        }
        protected override void HandleException(Exception exception) => throw new InvalidOperationException("Time input render failed", exception);
        protected override Task UpdateDisplayAsync(in RenderBatch batch)
        {
            for (var i = 0; i < batch.UpdatedComponents.Count; i++) _ids.Add(batch.UpdatedComponents.Array[i].ComponentId);
            return Task.CompletedTask;
        }
        private List<RenderTreeFrame> Attributes(string id)
        {
            foreach (var component in _ids)
            {
                var frames = GetCurrentRenderTreeFrames(component);
                for (var i = 0; i < frames.Count; i++)
                {
                    if (frames.Array[i].FrameType != RenderTreeFrameType.Element || frames.Array[i].ElementName != "input") continue;
                    var attributes = new List<RenderTreeFrame>();
                    for (var j = i + 1; j < frames.Count && frames.Array[j].FrameType == RenderTreeFrameType.Attribute; j++) attributes.Add(frames.Array[j]);
                    if (attributes.Any(f => f.AttributeName == "id" && Equals(f.AttributeValue, id))) return attributes;
                }
            }
            throw new InvalidOperationException($"Input not rendered: {id}");
        }
    }
}
#pragma warning restore BL0006
