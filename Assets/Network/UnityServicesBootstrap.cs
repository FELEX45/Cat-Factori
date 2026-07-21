using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

/// <summary>
/// Инициализация Unity Gaming Services и анонимный вход.
/// </summary>
public static class UnityServicesBootstrap
{
    static bool _signingIn;

    public static bool IsReady =>
        UnityServices.State == ServicesInitializationState.Initialized
        && AuthenticationService.Instance.IsSignedIn;

    public static async Task EnsureSignedInAsync()
    {
        if (IsReady)
            return;

        while (_signingIn)
            await Task.Yield();

        if (IsReady)
            return;

        _signingIn = true;
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[UGS] Signed in: {AuthenticationService.Instance.PlayerId}");
            }
        }
        finally
        {
            _signingIn = false;
        }
    }

    public static string DescribeError(Exception ex)
    {
        if (ex == null)
            return "Неизвестная ошибка";

        string msg = ex.Message;
        if (msg.IndexOf("project", StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("organization", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Привяжи Unity Project: Edit → Project Settings → Services";
        }

        return msg;
    }
}
