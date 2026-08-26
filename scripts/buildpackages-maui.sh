dotnet pack --configuration=Release src/Microcharts/Microcharts.csproj
dotnet pack --configuration=Release src/Microcharts.Maui/Microcharts.Maui.csproj
dotnet pack --configuration=Release src/Microcharts.Droid/Microcharts.Droid.csproj
dotnet pack --configuration=Release src/Microcharts.iOS/Microcharts.iOS.csproj
dotnet pack --configuration=Release src/Microcharts.Metapackage/Microcharts.Metapackage.csproj

dotnet build --configuration=Release example/Microcharts.Samples.Maui/Microcharts.Samples.Maui.csproj
dotnet build --configuration=Release example/Microcharts.Samples.Android/Microcharts.Samples.Android.csproj
dotnet build --configuration=Release example/Microcharts.Samples.iOS/Microcharts.Samples.iOS.csproj
