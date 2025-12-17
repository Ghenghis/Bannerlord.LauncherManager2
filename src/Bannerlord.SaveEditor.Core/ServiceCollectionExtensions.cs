// <copyright file="ServiceCollectionExtensions.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core;

using Bannerlord.SaveEditor.Core.Compression;
using Bannerlord.SaveEditor.Core.Interfaces;
using Bannerlord.SaveEditor.Core.Services;
using Bannerlord.SaveEditor.Core.Validation;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up Save Editor services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Save Editor core services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">An optional action to configure <see cref="SaveServiceOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddSaveEditorCore(
        this IServiceCollection services,
        Action<SaveServiceOptions>? configureOptions = null)
    {
        // Configure options
        var options = new SaveServiceOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton(options);

        // Register core services
        services.AddSingleton<IZlibHandler, ZlibHandler>();
        services.AddSingleton<IValidationService, ValidationService>();

        // Register editors
        services.AddTransient<CharacterEditor>();
        services.AddTransient<PartyEditor>();
        services.AddTransient<FleetEditor>();

        return services;
    }

    /// <summary>
    /// Adds Save Editor save service to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddSaveService(this IServiceCollection services)
    {
        services.AddSingleton<ISaveService, SaveService>();
        return services;
    }
}
