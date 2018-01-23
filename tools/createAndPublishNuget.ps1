echo "Creating Nuget Package"
&'..\tools\nuget.exe' 'pack' '..\src\MongoMigrations\MongoMigrations.csproj'
$nuget = ls "*.nupkg"
echo "Publishing Nuget Package"
&'..\tools\nuget.exe' 'push' $nuget.Name '-ApiKey' 'nuget:katon' '-Source' 'http://nuget.openposition.io/nuget/Katon'
rm $nuget.Name
echo "Done"
