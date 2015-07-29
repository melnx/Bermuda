using System;
using System.IO;

namespace Bermuda.ExpressionGeneration
{
    public class Input
    {
        public static void Main(string[] args)
        {
            MemoryStream stream = new MemoryStream();
            String saber = new String('c', 4);
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(@"
GET lol, troll, bla WHERE asdsadas lol test FROM:(bla NOT:lol OR (NOT:lolo OR ""lolol lolol"")) asdasd OR asdasd TO:lala OR (FROM:bla TO:(lol OR (BLA AND NOT:bla)) AND (NOT Lol OR BLA))");
            writer.Flush();

            Scanner scanner = new Scanner(stream);
            Parser parser = new Parser(scanner);
            MemoryStream errorStream = new MemoryStream();
            StreamWriter errorWriter = new StreamWriter(errorStream);

            parser.errors.errorStream = errorWriter;
            parser.Parse();
            errorWriter.Flush();
            errorStream.Seek(0, SeekOrigin.Begin);
            saber = new StreamReader(errorStream).ReadToEnd();
            errorStream.Close();
            stream.Close();
            if (parser.errors.count > 0)
            {
                Console.Out.WriteLine(saber);
            }
            else
            {
                PrintTree(parser.RootTree, 0);
            }
            Console.ReadLine();
        }

        public static void PrintTree(ExpressionTreeBase treeItem, int indent)
        {
            string add = "";
            for (int i = 0; i < indent; i++)
            {
                add += "  ";
            }
            Console.Out.WriteLine(add + treeItem);
            indent++;
            if (treeItem is GetExpression)
            {
                //Console.Out.WriteLine(add + "Types: ");
                //foreach (GetTypes current in ((GetExpression)treeItem).Types)
                //{
                    //Console.Out.WriteLine(add + current);
                //}
                Console.Out.WriteLine(add + "Conditions: ");
            }
            if (treeItem is MultiNodeTree)
            {
                foreach (ExpressionTreeBase current in ((MultiNodeTree)treeItem).Children)
                {
                    PrintTree(current, indent);
                }
            }
            else if (treeItem is SingleNodeTree)
            {
                PrintTree(((SingleNodeTree)treeItem).Child, indent);
            }
            if (treeItem is ConditionalExpression)
            {
                if (((ConditionalExpression)treeItem).AdditionalConditions.Count > 0)
                {
                    //Console.Out.WriteLine(add + "Additional Conditions:");
                    foreach (ConditionalExpression current in ((ConditionalExpression)treeItem).AdditionalConditions)
                    {
                        PrintTree(current, indent);
                    }
                }
            }
        }
    }
}
