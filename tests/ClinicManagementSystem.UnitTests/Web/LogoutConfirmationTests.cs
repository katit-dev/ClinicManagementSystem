using System.Reflection;
using ClinicManagementSystem.Application.DTOs.Auth;
using ClinicManagementSystem.Web.Services;
using ClinicManagementSystem.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moq;

namespace ClinicManagementSystem.UnitTests.Web;

public class LogoutConfirmationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HeaderLogout_OnlyClearsSessionAfterConfirmation(bool confirmed)
    {
        var fixture = new Fixture();
        fixture.Js.Setup(x => x.InvokeAsync<bool>("clinicLayout.confirmLogout", CancellationToken.None, It.IsAny<object?[]?>()))
            .ReturnsAsync(confirmed);

        await fixture.Logout();

        fixture.Js.Verify(x => x.InvokeAsync<bool>("clinicLayout.confirmLogout", CancellationToken.None, It.IsAny<object?[]?>()), Times.Once);
        if (confirmed)
        {
            Assert.Null(fixture.User.CurrentUser);
            Assert.True(string.IsNullOrEmpty(fixture.User.AccessToken));
            Assert.Equal("http://fixture.local/login", fixture.Navigation.Uri);
            foreach (var key in new[] { "accessToken", "refreshToken", "currentUser" })
                fixture.Storage.Verify(x => x.RemoveItemAsync(key), Times.Once);
            Assert.False((await fixture.Authentication.GetAuthenticationStateAsync()).User.Identity!.IsAuthenticated);
        }
        else
        {
            Assert.Equal(7, fixture.User.CurrentUser!.Id);
            Assert.Equal("fixture-access-token", fixture.User.AccessToken);
            Assert.Equal("http://fixture.local/admin", fixture.Navigation.Uri);
            fixture.Storage.Verify(x => x.RemoveItemAsync(It.IsAny<string>()), Times.Never);
            Assert.True((await fixture.Authentication.GetAuthenticationStateAsync()).User.Identity!.IsAuthenticated);
        }
    }

    [Fact]
    public async Task RepeatedClick_WhileConfirmationPending_DoesNotOpenAnotherDialog()
    {
        var fixture = new Fixture();
        var decision = new TaskCompletionSource<bool>();
        fixture.Js.Setup(x => x.InvokeAsync<bool>("clinicLayout.confirmLogout", CancellationToken.None, It.IsAny<object?[]?>()))
            .Returns(() => new ValueTask<bool>(decision.Task));

        var pending = fixture.Logout();
        await fixture.Logout();
        fixture.Js.Verify(x => x.InvokeAsync<bool>("clinicLayout.confirmLogout", CancellationToken.None, It.IsAny<object?[]?>()), Times.Once);
        Assert.NotNull(fixture.User.CurrentUser);

        decision.SetResult(false);
        await pending;
        await fixture.Logout();
        fixture.Js.Verify(x => x.InvokeAsync<bool>("clinicLayout.confirmLogout", CancellationToken.None, It.IsAny<object?[]?>()), Times.Exactly(2));
        fixture.Storage.Verify(x => x.RemoveItemAsync(It.IsAny<string>()), Times.Never);
    }

    private sealed class Fixture
    {
        public Mock<IJSRuntime> Js { get; } = new();
        public Mock<ILocalStorageService> Storage { get; } = new();
        public FixtureNavigation Navigation { get; } = new();
        public CustomAuthenticationStateProvider Authentication { get; } = new();
        public UserStateService User { get; }
        private readonly HomePageMaster _layout = new();

        public Fixture()
        {
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
            User = new(Storage.Object, factory.Object, Navigation, Authentication);
            var actor = new AuthUserDTO { Id = 7, FullName = "Admin Test", Roles = ["Admin"] };
            typeof(UserStateService).GetProperty(nameof(UserStateService.CurrentUser))!.SetValue(User, actor);
            typeof(UserStateService).GetProperty(nameof(UserStateService.AccessToken))!.SetValue(User, "fixture-access-token");
            Authentication.MarkUserAsAuthenticated(actor);
            typeof(HomePageMaster).GetProperty("_userStateService", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_layout, User);
            typeof(HomePageMaster).GetProperty("JS", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_layout, Js.Object);
        }

        public Task Logout() => (Task)typeof(HomePageMaster)
            .GetMethod("HandleLogout", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_layout, null)!;
    }

    private sealed class FixtureNavigation : NavigationManager
    {
        public FixtureNavigation() => Initialize("http://fixture.local/", "http://fixture.local/admin");
        protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).ToString();
    }
}
