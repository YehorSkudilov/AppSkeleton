# AppSkeleton

The shared .NET MAUI building blocks of the apps: swipeable tabs (`CView`) with a glass nav bar (`CNavBar`,
`AcrylicView`), pull to refresh that works with the swipe, charts, buttons, input fields and the Material Icons fonts.

Used by [Gym Book](https://github.com/YehorSkudilov/GymBook), [BudgetBuddy](https://github.com/YehorSkudilov/BudgetBuddy) and
[StudyJit](https://github.com/YehorSkudilov/StudyJit).
Each app references it as a project, `..\..\AppSkeleton\AppSkeleton\AppSkeleton.csproj`, so clone it next to the app's
repository; the apps' publish workflows check it out the same way (with the `REPO_TOKEN` secret).

```csharp
builder
    .UseMauiApp<App>()
    .UseAppSkeleton()
    .ConfigureFonts(fonts => fonts.AddCIcons()); // "RegularIcons" and "OutlinedIcons"
```

Android builds on any OS; iOS and Mac Catalyst are only targeted on a Mac or Windows, and Windows only on Windows.

## Onboarding and sign-in pieces

Shared by Gym Book's and BudgetBuddy's first-run screens and sign-in forms. Each takes its colours from the app, through
an implicit style (see Gym Book's `Resources/Styles/Styles.xaml` and BudgetBuddy's `Resources/Styles/Theme.xaml`).

- `CFeatureCard`: one thing the app does, for a welcome screen (optional Material icon, title, line).
- `CChoiceCard`: one answer in a list of choices, outlined while `IsSelected`; a tap runs `Command` / raises `Tapped`.
- `COrDivider`: "—— or ——" between a form and the buttons under it.
- `CResendTimer`: a "Resend code (0:42)" countdown for emailed codes. Set `CResendTimer.Format` once to translate it.
- `ProgressLine`: a drawn progress bar (MAUI's ProgressBar crashed WinUI at some scalings).
- `AnimatedDrawingView`: base for small decorations drawn in code that animate; `TimeOfDayView` is a sun/moon scene
  for a greeting. An app that changes its animation speed sets `AnimatedDrawingView.LoopLength` to keep their pace.
