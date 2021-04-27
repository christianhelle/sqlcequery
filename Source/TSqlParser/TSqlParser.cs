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
                bool b_error = false;
                int cur_char_idx = 0;
                foreach (var batch in ctxt?.children) {
                    for (int i = 0; i < batch.ChildCount && !b_error; i++) {
                        var stmt_tree = batch.GetChild(i);
                        var stmt = stmt_tree.Payload as TSqlParserCore.Sql_clausesContext;

                        int start_idx;
                        int stop_idx;
                        if (stmt is null) {
                            b_error = true;
                            start_idx = cur_char_idx;
                            stop_idx = s.Length - 1;
                        } else {
                            start_idx = stmt.Start.StartIndex;

                            if (stmt.Stop is null) {
                                b_error = true;
                                stop_idx = s.Length - 1;
                            } else {
                                stop_idx = stmt.Stop.StopIndex;
                                cur_char_idx = stop_idx + 1;
                            }
                        }

                        var stmt_s = s.Substring(start_idx, stop_idx - start_idx + 1);
                        stmts.Add(stmt_s);
                    }
                    if (b_error) {
                        break;
                    }
                }
            }

            return stmts;
        }
    }
}
