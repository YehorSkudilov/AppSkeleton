# AppSkeleton

The shared .NET MAUI building blocks of the apps: swipeable tabs (`CView`) with a glass nav bar (`CNavBar`,
`AcrylicView`), pull to refresh that works with the swipe, charts, buttons, input fields and the Material Icons fonts.

Used by [Gym Book](https://github.com/YehorSkudilov/GymBook) and [StudyJit](https://github.com/YehorSkudilov/StudyJit).
Each app references it as a project, `..\..\AppSkeleton\AppSkeleton\AppSkeleton.csproj`, so clone it next to the app's
repository; the apps' publish workflows check it out the same way (with the `REPO_TOKEN` secret).

```csharp
builder
    .UseMauiApp<App>()
    .UseAppSkeleton()
    .ConfigureFonts(fonts => fonts.AddCIcons()); // "RegularIcons" and "OutlinedIcons"
```

Android builds on any OS; iOS and Mac Catalyst are only targeted on a Mac or Windows, and Windows only on Windows.
