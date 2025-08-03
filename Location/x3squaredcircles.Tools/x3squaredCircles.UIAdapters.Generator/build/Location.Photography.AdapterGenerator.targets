<Project>
  
  <!-- Only run in Debug builds -->
  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <RunPhotographyAdapterGenerator Condition="'$(RunPhotographyAdapterGenerator)' == ''">true</RunPhotographyAdapterGenerator>
  </PropertyGroup>

  <!-- Define the Photography Adapter Generator task -->
  <Target Name="GeneratePhotographyAdapters" 
          AfterTargets="Build" 
          Condition="'$(RunPhotographyAdapterGenerator)' == 'true'">
    
    <PropertyGroup>
      <!-- Default to global tool, fallback options available -->
      <AdapterGeneratorCommand Condition="'$(AdapterGeneratorCommand)' == ''">photography-viewmodel-generator</AdapterGeneratorCommand>
      <AdapterGeneratorArgs Condition="'$(AdapterGeneratorArgs)' == ''">--verbose</AdapterGeneratorArgs>
      
      <!-- Allow MSBuild property overrides -->
      <AdapterGeneratorArgs Condition="'$(AdapterGeneratorPlatform)' != ''">$(AdapterGeneratorArgs) --platform $(AdapterGeneratorPlatform)</AdapterGeneratorArgs>
      <AdapterGeneratorArgs Condition="'$(AdapterGeneratorOutput)' != ''">$(AdapterGeneratorArgs) --output "$(AdapterGeneratorOutput)"</AdapterGeneratorArgs>
      <AdapterGeneratorArgs Condition="'$(AdapterGeneratorCoreAssembly)' != ''">$(AdapterGeneratorArgs) --core-assembly "$(AdapterGeneratorCoreAssembly)"</AdapterGeneratorArgs>
      <AdapterGeneratorArgs Condition="'$(AdapterGeneratorPhotographyAssembly)' != ''">$(AdapterGeneratorArgs) --photography-assembly "$(AdapterGeneratorPhotographyAssembly)"</AdapterGeneratorArgs>
    </PropertyGroup>

    <!-- Skip if this IS the adapter generator project -->
    <PropertyGroup Condition="'$(MSBuildProjectName)' == 'PhotographyAdapterGenerator'">
      <RunPhotographyAdapterGenerator>false</RunPhotographyAdapterGenerator>
    </PropertyGroup>

    <Message Text="[PhotographyAdapterGenerator] Running adapter generation after Debug build..." 
             Importance="high" 
             Condition="'$(RunPhotographyAdapterGenerator)' == 'true'" />
    <Message Text="[PhotographyAdapterGenerator] Project: $(MSBuildProjectName)" 
             Importance="normal" 
             Condition="'$(RunPhotographyAdapterGenerator)' == 'true'" />
    <Message Text="[PhotographyAdapterGenerator] Command: $(AdapterGeneratorCommand) $(AdapterGeneratorArgs)" 
             Importance="normal" 
             Condition="'$(RunPhotographyAdapterGenerator)' == 'true'" />
    
    <!-- Execute the generator -->
    <Exec Command="$(AdapterGeneratorCommand) $(AdapterGeneratorArgs)" 
          ContinueOnError="false"
          WorkingDirectory="$(MSBuildProjectDirectory)" 
          Condition="'$(RunPhotographyAdapterGenerator)' == 'true'" />
    
    <Message Text="[PhotographyAdapterGenerator] Adapter generation completed successfully!" 
             Importance="high" 
             Condition="'$(RunPhotographyAdapterGenerator)' == 'true'" />
  </Target>

</Project>