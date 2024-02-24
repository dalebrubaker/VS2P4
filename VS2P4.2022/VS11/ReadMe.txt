Instructions for converting the generated .vsix file to support Visual Studio 2012 and 2013 and 2015

1. Raname .vsix to .zip and unzip it
2. Edit the extension.vsixmanifest file with Notepad
3. Under <SupportedProducts>, BEFORE the opening of <VisualStudio Version="10.0">, add:
<VisualStudio Version="14.0"> 
  <Edition>Enterprise</Edition> 
  <Edition>Professional</Edition> 
  <Edition>Community</Edition> 
  <Edition>IntegratedShell</Edition> 
</VisualStudio>
<VisualStudio Version="12.0"> 
  <Edition>Ultimate</Edition> 
  <Edition>Premium</Edition> 
  <Edition>Pro</Edition> 
  <Edition>IntegratedShell</Edition> 
</VisualStudio>
<VisualStudio Version="11.0"> 
  <Edition>Ultimate</Edition> 
  <Edition>Premium</Edition> 
  <Edition>Pro</Edition> 
  <Edition>IntegratedShell</Edition> 
</VisualStudio>
4. Send the files to a .zip file
5. Rename the .zip to .vsix

Thanks to: http://gurustop.net/blog/2011/09/20/running-dev11-vs11-visual_studio_11-extensions-extension_manager_gallery-git-scm-plugin-addon/
