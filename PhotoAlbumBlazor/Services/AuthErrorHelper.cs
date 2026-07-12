using System.Net;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace PhotoAlbumBlazor.Services;

// The API rejects requests with an expired/invalid JWT as 401. Every page
// used to let that surface as an unhandled HttpRequestException, which
// crashes the whole WASM render tree instead of sending the user back to
// login. Centralized here since the same handling belongs on every
// authenticated call site.
public static class AuthErrorHelper
{
    public static bool IsUnauthorized(this Exception ex) =>
        ex is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized };

    public static async Task HandleUnauthorizedAsync(ILocalStorageService localStorage, NavigationManager navigation)
    {
        await localStorage.RemoveItemAsync("authToken");
        navigation.NavigateTo("/login", forceLoad: true);
    }
}
