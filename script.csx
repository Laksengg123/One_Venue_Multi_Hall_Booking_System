using System;
using System.IO;

var files = Directory.GetFiles(""Features"", ""*.cs"", SearchOption.AllDirectories);
foreach (var file in files) {
    var content = File.ReadAllText(file);
    content = content.Replace(""Console.WriteLine(\""  "", ""Console.WriteLine(ConsoleHelper.GetIndent() + \"""");
    content = content.Replace(""Console.Write(\""  "", ""Console.Write(ConsoleHelper.GetIndent() + \"""");
    File.WriteAllText(file, content);
}
var progContent = File.ReadAllText(""Program.cs"");
progContent = progContent.Replace(""Console.WriteLine(\""  "", ""Console.WriteLine(ConsoleHelper.GetIndent() + \"""");
progContent = progContent.Replace(""Console.Write(\""  "", ""Console.Write(ConsoleHelper.GetIndent() + \"""");
File.WriteAllText(""Program.cs"", progContent);
