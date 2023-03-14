using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Json
{
    public class JsonAnalyzer
    {
        public string src = "";
        public JsonNode body;

        public class JsonNode
        {
            public enum Type
            {
                None,
                String,
                Int,
                Bool,
                Value,          // 何らかの値
                Vector,         // 配列
                Dictionary,     // 辞書
            }
            public Type type = Type.None;
            public string name;
            public object value;

            List<JsonNode> items;
            List<Token> tokens;

            public void Analyze(List<Token> tokens)
            {
                this.tokens = tokens;

                var p1 = tokens[0];
                var p2 = tokens[tokens.Count - 1];
                if ((p1.type == Token.Type.Operator) && (p1.text == "{") && (p2.type == Token.Type.Operator) && (p2.text == "}"))
                {
                    // 辞書
                    items = new List<JsonNode>();

                    var tmpTokens = new List<Token>();
                    var num = tokens.Count - 1;
                    for (var i = 1; i < num; i++) tmpTokens.Add(tokens[i]);

                    num = tmpTokens.Count;
                    for (var i = 0; i < num; i++)
                    {
                        var t1 = tmpTokens[i];

                        if (t1.type == Token.Type.String)
                        {
                            if (tmpTokens.Count <= i + 1) continue;
                            var t2 = tmpTokens[i + 1];
                            if ((t2.type == Token.Type.Operator) && (t2.text == ":"))
                            {
                                var t3 = tmpTokens[i + 2];
                                if ((t3.type == Token.Type.Operator) && (t3.text == "{"))
                                {
                                    // 辞書型
                                    var newNode = new JsonNode()
                                    {
                                        name = TrinStringDQ(t1.text),
                                        type = Type.Dictionary
                                    };
                                    items.Add(newNode);

                                    i += 3;
                                    AnalyzeDictionaryValue(ref i, newNode, t3, num, tmpTokens);
                                }
                                else if ((t3.type == Token.Type.Operator) && (t3.text == "["))
                                {
                                    // 配列
                                    var newNode = new JsonNode()
                                    {
                                        name = TrinStringDQ(t1.text),
                                        type = Type.Vector
                                    };
                                    items.Add(newNode);

                                    i += 3;
                                    AnalyzeVectorValue(ref i, newNode, t3, num, tmpTokens);

                                }
                                else if (t3.type == Token.Type.String)
                                {
                                    // 文字列、数値、bool など
                                    var newNode = new JsonNode()
                                    {
                                        name = TrinStringDQ(t1.text),
                                        type = Type.Value
                                    };
                                    items.Add(newNode);

                                    AnalyzeValue(newNode, t3);
                                    i += 1;
                                }

                            }
                        }
                    }

                }
                else if ((p1.type == Token.Type.Operator) && (p1.text == "[") && (p2.type == Token.Type.Operator) && (p2.text == "]"))
                {
                    // 配列
                    items = new List<JsonNode>();

                    var tmpTokens = new List<Token>();
                    var num = tokens.Count - 1;
                    for (var i = 1; i < num; i++) tmpTokens.Add(tokens[i]);

                    num = tmpTokens.Count;

                    // 配列なので、辞書、配列、値 の3種類の連続に限定され、 , で区切られている

                    for (var i = 0; i < num; i++)
                    {
                        var t1 = tmpTokens[i];

                        var t3 = tmpTokens[i];
                        if ((t3.type == Token.Type.Operator) && (t3.text == "{"))
                        {
                            // 辞書型
                            var newNode = new JsonNode()
                            {
                                type = Type.Dictionary
                            };
                            items.Add(newNode);

                            i += 1;
                            AnalyzeDictionaryValue2(ref i, newNode, t3, num, tmpTokens);
                        }
                        else if ((t3.type == Token.Type.Operator) && (t3.text == "["))
                        {
                            // 配列
                            var newNode = new JsonNode()
                            {
                                type = Type.Vector
                            };
                            items.Add(newNode);

                            i += 1;
                            AnalyzeVectorValue(ref i, newNode, t3, num, tmpTokens);

                        }
                        else if (t3.type == Token.Type.String)
                        {
                            // 文字列、数値、bool など
                            var newNode = new JsonNode()
                            {
                                type = Type.Value
                            };
                            items.Add(newNode);

                            AnalyzeValue(newNode, t3);
                        }
                    }
                }
            }

            private void AnalyzeDictionaryValue( ref int i, JsonNode newNode, Token t3, int num, List<Token> tmpTokens )
            {
                var start = i;
                var depth = 0;
                var jEnd = 0;
                newNode.tokens = new List<Token>();
                newNode.tokens.Add(t3);
                for (var j = start; j < num; j++)
                {
                    jEnd = j;
                    var tj1 = tmpTokens[j];
                    if ((tj1.type == Token.Type.Operator) && (tj1.text == "{"))
                    {
                        depth++;
                        newNode.tokens.Add(tj1);
                    }
                    else if ((tj1.type == Token.Type.Operator) && (tj1.text == "}"))
                    {
                        if (depth == 0)
                        {
                            newNode.tokens.Add(tj1);
                            break;
                        }
                        else
                        {
                            newNode.tokens.Add(tj1);
                            depth--;
                        }
                    }
                    else
                    {
                        newNode.tokens.Add(tj1);
                    }
                }
                i = jEnd;

                newNode.Analyze(newNode.tokens);
            }

            private void AnalyzeDictionaryValue2(ref int i, JsonNode newNode, Token t3, int num, List<Token> tmpTokens)
            {
                var start = i;
                var depth = 0;
                var jEnd = 0;
                newNode.tokens = new List<Token>();
                newNode.tokens.Add(t3);
                for (var j = start; j < num; j++)
                {
                    jEnd = j;
                    var tj1 = tmpTokens[j];
                    if ((tj1.type == Token.Type.Operator) && (tj1.text == "{"))
                    {
                        depth++;
                        newNode.tokens.Add(tj1);
                    }
                    else if ((tj1.type == Token.Type.Operator) && (tj1.text == "}"))
                    {
                        if (depth == 0)
                        {
                            newNode.tokens.Add(tj1);
                            break;
                        }
                        else
                        {
                            newNode.tokens.Add(tj1);
                            depth--;
                        }
                    }
                    else
                    {
                        newNode.tokens.Add(tj1);
                    }
                }
                i = jEnd;

                newNode.Analyze(newNode.tokens);
            }

            private void AnalyzeVectorValue(ref int i, JsonNode newNode, Token t3, int num, List<Token> tmpTokens)
            {
                var start = i;
                var depth = 0;
                var jEnd = 0;
                newNode.tokens = new List<Token>();
                newNode.tokens.Add(t3);
                for (var j = start; j < num; j++)
                {
                    jEnd = j;
                    var tj1 = tmpTokens[j];
                    if ((tj1.type == Token.Type.Operator) && (tj1.text == "["))
                    {
                        depth++;
                        newNode.tokens.Add(tj1);
                    }
                    else if ((tj1.type == Token.Type.Operator) && (tj1.text == "]"))
                    {
                        if (depth == 0)
                        {
                            newNode.tokens.Add(tj1);
                            break;
                        }
                        else
                        {
                            newNode.tokens.Add(tj1);
                            depth--;
                        }
                    }
                    else
                    {
                        newNode.tokens.Add(tj1);
                    }
                }
                i = jEnd;

                newNode.Analyze(newNode.tokens);

            }

            private void AnalyzeValue( JsonNode newNode, Token t3)
            {
                if ( t3.text.IndexOf("\"")==0 )
                {
                    // 文字列
                    newNode.type = Type.String;
                    newNode.value = TrinStringDQ(t3.text);
                }
                else if (t3.text == "true")
                {
                    newNode.type = Type.Bool;
                    newNode.value = true;
                }
                else if (t3.text == "false")
                {
                    newNode.type = Type.Bool;
                    newNode.value = false;
                }
                else
                {
                    // 数値の可能性が高い
                    // 整数としていったん処理する
                    try
                    {
                        newNode.type = Type.Int;
                        newNode.value = Int64.Parse(t3.text);
                    }
                    catch
                    {
                        newNode.type = Type.String;
                        newNode.value = t3.text;
                    }
                }

            }

            // " があれば削除する
            static public string TrinStringDQ( string text)
            {
                if (text.Length == 0) return text;
                if (text[0]=='"')
                {
                    if (text[text.Length-1] == '"')
                    {
                        return text.Substring(1, text.Length - 2);
                    }
                }
                return text;
            }

            public string Debug(string depthText, bool isFirst)
            {
                var tmp = "";

                if (type == Type.Dictionary)
                {
                    if(isFirst) tmp += $"{depthText}" + "{\n";
                    foreach (var v in items)
                    {
                        if (v.type == Type.Dictionary)
                        {
                            tmp += $"{depthText}\t\"{v.name}\": "+"{\n";
                            tmp += v.Debug(depthText + "\t", false);
                            tmp += $"{depthText}\t" + "},\n";
                        }
                        else if (v.type == Type.Vector)
                        {
                            tmp += $"{depthText}\t\"{v.name}\": "+"[\n";
                            tmp += v.Debug(depthText + "\t", false);
                            tmp += $"{depthText}\t" + "],\n";
                        }
                        else if (v.type == Type.String)
                        {
                            tmp += $"{depthText}\t\"{v.name}\": \"{(string)v.value}\",\n";
                        }
                        else if (v.type == Type.Int)
                        {
                            tmp += $"{depthText}\t\"{v.name}\": {(Int64)v.value},\n";
                        }
                        else if (v.type == Type.Bool)
                        {
                            if ( (bool)v.value == true)
                            {
                                tmp += $"{depthText}\t\"{v.name}\": true,\n";
                            }
                            else
                            {
                                tmp += $"{depthText}\t\"{v.name}\": false,\n";
                            }
                        }
                    }
                    if (isFirst) tmp += $"{depthText}"+"}\n";
                }
                else if (type == Type.Vector)
                {
                    if (items == null) return ""; 
                    foreach (var v in items)
                    {
                        if (v.type == Type.Dictionary)
                        {
                            tmp += $"{depthText}\t" + "{\n";
                            tmp += v.Debug(depthText + "\t", false);
                            tmp += $"{depthText}\t" + "},\n";
                        }
                        else if (v.type == Type.Dictionary)
                        {
                            tmp += $"{depthText}\t" + "{\n";
                            tmp += v.Debug(depthText + "\t", false);
                            tmp += $"{depthText}\t" + "],\n";
                        }
                        else if (v.type == Type.String)
                        {
                            tmp += $"{depthText}{(string)v.value}\",\n";
                        }
                        else if (v.type == Type.Int)
                        {
                            tmp += $"{depthText}{(Int64)v.value},\n";
                        }
                        else if (v.type == Type.Bool)
                        {
                            if ((bool)v.value == true)
                            {
                                tmp += $"{depthText}true,\n";
                            }
                            else
                            {
                                tmp += $"{depthText}false,\n";
                            }
                        }
                    }

                }

                return tmp;
            }

            public JsonNode GetNode( string key )
            {
                if (this == null) return null;

                if ( type == Type.Dictionary )
                {
                    foreach ( var v in items )
                    {
                        if (v.name == key)
                        {
                            return v;
                        }
                    }
                }
                return null;
            }

            public JsonNode GetNode(params string[] keys)
            {
                if (keys.Length==1)
                {
                    return GetNode(keys[0]);
                }

                var tmp = new string[keys.Length - 1];
                for (var i = 1; i < keys.Length; i++) tmp[i - 1] = keys[i];

                var nextNode = GetNode(keys[0]);
                if (nextNode == null) return null;

                return nextNode.GetNode(tmp);
            }

            public JsonNode GetNode( int i)
            {
                return items[i];
            }

            public int Count()
            {
                return items.Count();
            }
        }

        public class Token
        {
            public enum Type
            {
                None,
                String,
                Operator,
            }

            public string text;
            public Type type;
        }
        

        public JsonAnalyzer( string src )
        {
            this.src = src;
            body = new JsonNode() { name = "body", type = JsonNode.Type.Dictionary };

            var tokens = ConvertTokensByText(src);

            body.Analyze(tokens);

            //var tmp = body.Debug("", true);
            //tmp = tmp.Replace("\t", "    ");
            //using ( var sw = new System.IO.StreamWriter("out.json", false, Encoding.UTF8))
            //{
            //    sw.Write(tmp);
            //}
        }


        private List<Token> ConvertTokensByText( string src )
        {
            var tokens = new List<Token>();

            var num = src.Length;
            var isStringRead = false;
            var tmpString = "";
            for (var i = 0; i < num; i++)
            {
                var p1 = src[i].ToString();

                if (isStringRead)
                {
                    if (p1 == "\"")
                    {
                        isStringRead = false;
                        tmpString += p1;

                        tokens.Add(new Token() { text = tmpString, type = Token.Type.String });
                        tmpString = "";
                    }
                    else
                    {
                        tmpString += p1;
                    }
                }
                else
                {
                    if (p1 == "\"")
                    {
                        isStringRead = true;

                        // 本来ここで tmpString  にすでに入力があるとおかしい

                        tmpString += p1;

                    }
                    else if ((p1 == " ") || (p1 == "\n") || (p1 == "\r") || (p1 == "\0") || (p1 == "\t"))
                    {
                        // Skip
                    }
                    else if ("{}[]:,".IndexOf(p1) >= 0)
                    {
                        if (tmpString.Length > 0) tokens.Add(new Token() { text = tmpString, type = Token.Type.String });
                        tokens.Add(new Token() { text = p1, type = Token.Type.Operator });

                        tmpString = "";
                    }
                    else
                    {
                        tmpString += p1;
                    }
                }
            }
            return tokens;
        }

    }
}
