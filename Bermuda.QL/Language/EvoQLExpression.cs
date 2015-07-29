using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Bermuda.QL.Language;

namespace Bermuda.QL
{
    public static class GetTypesExtensions
    {
        public static bool IsBaseOf(this GetTypes me, GetTypes comparison)
        {
            var smallestType = (GetTypes)((int)me & (int)comparison);

            return smallestType == me;
        }
    }

    public class EvoQLExpression
    {
        public bool HadErrors { get; private set; }

        public string[] Errors { get; private set; }

        public RootExpression Tree { get; private set; }

        public EvoQLExpression(string query)
            : this(query, null)
        {

        }

        private void AddError(string error)
        {
            if (Errors == null)
            {
                Errors = new string[] { error };
            }
            else
            {
                List<string> errors = Errors.ToList();
                errors.Add(error);
                Errors = errors.ToArray();
            }
        }

        public EvoQLExpression(string query, IEnumerable<GetTypes> defaultTypes)
        {
            MemoryStream stream = new MemoryStream();
            String errorString;
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(query);
            writer.Flush();

            Scanner scanner = new Scanner(stream);
            Parser parser = new Parser(scanner);
            MemoryStream errorStream = new MemoryStream();
            StreamWriter errorWriter = new StreamWriter(errorStream);

            parser.errors.errorStream = errorWriter;
            parser.Parse();
            errorWriter.Flush();
            errorStream.Seek(0, SeekOrigin.Begin);
            errorString = new StreamReader(errorStream).ReadToEnd();
            errorStream.Close();
            stream.Close();

            if (parser.errors.count > 0)
            {
                Errors = errorString.Split('\n');
                HadErrors = true;
            }
            else
            {
                Tree = parser.RootTree;
            }
        }

    }
}
