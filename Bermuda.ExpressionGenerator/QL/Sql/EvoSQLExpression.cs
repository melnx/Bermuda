using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Bermuda.Core.QL.Sql
{

    public class EvoSQLExpression
    {
        public bool HadErrors { get; private set; }

        public string[] Errors { get; private set; }

        public RootExpression Tree { get; private set; }

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

        public EvoSQLExpression(string query)
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
