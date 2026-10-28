using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
var asm=AssemblyDefinition.ReadAssembly(args[0]);
foreach(var method in asm.MainModule.Types[0].Methods) if(method.Name==".cctor"){
Console.WriteLine("Test isolation: removing Windows bootstrap module initializer from test copy only");
method.Body.Instructions.Clear();method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));method.Body.ExceptionHandlers.Clear();method.Body.Variables.Clear();}
asm.Write(args[1]);
