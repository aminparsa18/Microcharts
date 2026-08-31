dotnet pack --configuration=Release src/Plugin.Maui.Microchart.Core/Plugin.Maui.Microchart.Core.csproj
dotnet pack --configuration=Release src/Plugin.Maui.Microchart/Plugin.Maui.Microchart.csproj
dotnet pack --configuration=Release src/Plugin.Maui.Microchart.Droid/Plugin.Maui.Microchart.Droid.csproj
dotnet pack --configuration=Release src/Plugin.Maui.Microchart.iOS/Plugin.Maui.Microchart.iOS.csproj
dotnet pack --configuration=Release src/Plugin.Maui.Microchart.Metapackage/Plugin.Maui.Microchart.Metapackage.csproj

dotnet build --configuration=Release example/Plugin.Maui.Microchart.Samples.Maui/Plugin.Maui.Microchart.Samples.Maui.csproj
dotnet build --configuration=Release example/Plugin.Maui.Microchart.Samples.Android/Plugin.Maui.Microchart.Samples.Android.csproj
dotnet build --configuration=Release example/Plugin.Maui.Microchart.Samples.iOS/Plugin.Maui.Microchart.Samples.iOS.csproj
