using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace ChristianHelle.DatabaseTools.SqlCe.TSqlParser {
    class TSqlParser {

        public static List<string> statements_from_str(string s) {
            var stmts = new List<string>();

            var stream = new AntlrInputStream(s.ToUpper());
            ITokenSource lexer = new TSqlLexerCore(stream);
            ITokenStream tokens = new CommonTokenStream(lexer);
            TSqlParserCore parser = new TSqlParserCore(tokens);
            parser.RemoveErrorListeners();
            parser.RemoveParseListeners();
            //parser.BuildParseTree = true;

            if (parser.tsql_file() is var ctxt) {
                foreach (var batch in ctxt?.children) {
                    for (int i = 0; i < batch.ChildCount; i++) {
                        var stmt = batch.GetChild(i);

                        var stmt_s = "";
                        var prefix = "";
                        for (int j = stmt.SourceInterval.a; j <= stmt.SourceInterval.b; j++) {
                            stmt_s += $"{prefix}{tokens.Get(j).Text}";
                            prefix = " ";
                        }

                        stmts.Add(stmt_s);
                    }
                }
            }

            return stmts;
        }
    }
}
