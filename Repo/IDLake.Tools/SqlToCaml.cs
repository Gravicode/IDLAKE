using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDLake.Tools
{
    public static class SqlToCAML
    {
        public static string TextSqlToCAML(string query)
        {
            string ret = "";
            try
            {
                string[] grpsExpr = query.ToLower().Split(new string[] { "select", "from", "where", "order by", "having" }, StringSplitOptions.RemoveEmptyEntries);
                ret += TextSqlToCAML(getValueStrArr(grpsExpr, 0),
                                     getValueStrArr(grpsExpr, 1),
                                     getValueStrArr(grpsExpr, 2),
                                     getValueStrArr(grpsExpr, 3),
                                     getValueStrArr(grpsExpr, 4)
                                    );
            }
            catch (Exception ex)
            {
                Log("CSqlToCAML.TextSqlToCAML() error: " + ex.Message);
            }
            return ret;
        }

        public static string TextSqlToCAML(string select, string from, string where, string orderby, string having)
        {
            string ret = "<Query>";
            try
            {
                ret += sqltocamlSelect(select);
                ret += sqltocamlWhere(where);
                ret += sqltocamlOrderBy(orderby);
            }
            catch (Exception ex)
            {
                Log("CSqlToCAML.TextSqlToCAML() error: " + ex.Message);
            }
            return ret + "</Query>";
        }


        private static string getValueStrArr(string[] strs, int index)
        {
            try
            {
                return strs[index];
            }
            catch
            {
                return "";
            }
        }

        private static string sqltocamlOrderBy(string _orderby)
        {
            string ret = "";
            try
            {
                ret += "<OrderBy>\n";
                string[] grpsExpr = _orderby.ToLower().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string expr in grpsExpr)
                {
                    string val = expr.ToLower();
                    string ascc = val.ToLower().Contains("asc") ? "TRUE" : val.ToLower().Contains("desc") ? "FALSE" : "TRUE";
                    val = val.Replace("asc", "");
                    val = val.Replace("desc", "");
                    val = val.Trim();
                    ret += string.Format("<FieldRef Name=\"{0}\" Ascending=\"{1}\" />\n", val, ascc).Trim();
                }
                ret += "</OrderBy>\n";
            }
            catch (Exception ex)
            {
                Log("CSqlToCAML.sqltocamlSelect() error: " + ex.Message);
            }
            return ret;
        }

        private static string sqltocamlSelect(string _select)
        {
            string ret = "";
            try
            {
                ret += "<ViewFields>\n";
                string[] grpsExpr = _select.ToLower().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string expr in grpsExpr)
                {
                    ret += string.Format("<FieldRef Name=\"{0}\" />\n", expr).Trim();
                }
                ret += "</ViewFields>\n";
            }
            catch (Exception ex)
            {
                Log("CSqlToCAML.sqltocamlSelect() error: " + ex.Message);
            }
            return ret;
        }

        private static string sqltocamlWhere(string _where)
        {
            string ret = "", retAnd = "", retOr = "";
            try
            {
                /*
                •Eq = equal to  
                •Neq = not equal to 
                •BeginsWith = begins with 
                •Contains = contains 
                •Lt = less than 
                •Leq = less than or equal to
                •Gt = greater than 
                •Geq = greater than or equal to 
                •IsNull = is null 
                •IsNotNull = is not null
                */

                // "(id_typ = 3 or id_typ = 4) and (datum_zal > datum_odos) "
                ret += "<Where>\n";
                string[] grpsExpr = _where.ToLower().Split(new string[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string expr in grpsExpr)
                {

                    if (expr.Contains("and"))
                    {
                        retAnd = "";
                        foreach (string exp in expr.Split(new string[] { "and" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            retAnd += expStr(exp);
                        }
                        if (retAnd.Length > 0)
                        {
                            ret += "<And>\n";
                            ret += retAnd;
                            ret += "</And>\n";
                        }
                    }

                    if (expr.Contains("or") != null)
                    {
                        retOr = "";
                        foreach (string exp in expr.Split(new string[] { "or" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            retOr += expStr(exp);
                        }
                        if (retOr.Length > 0)
                        {
                            ret += "<Or>\n";
                            ret += retOr;
                            ret += "</Or>\n";
                        }
                    }
                }
                ret += "</Where>\n";
            }
            catch (Exception ex)
            {
                Log("CSqlToCAML.sqltocamlWhere() error: " + ex.Message);
            }
            return ret;
        }

        private static string expStr(string exp)
        {
            string ret = "";
            ret += propExp(exp, "=");
            ret += propExp(exp, "<>");
            ret += propExp(exp, "<");
            ret += propExp(exp, ">");
            ret += propExp(exp, "<=");
            ret += propExp(exp, ">=");
            ret += propExp(exp, "is null");
            ret += propExp(exp, "is not null");
            ret += propExp(exp, "in");
            ret += propExp(exp, "like");
            ret += propExp(exp, "between");
            return ret;
        }


        private static string propExp(string sExp, string op)
        {
            string ret = "", _op = "";
            try
            {
                if (!sExp.Contains(op))
                    return "";
                sExp = sExp.Replace("'", " ");
                sExp = sExp.Replace("   ", " ");
                sExp = sExp.Replace("  ", " ");
                string[] _ops = sExp.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                string[] _opx = sExp.Split(new string[] { op }, StringSplitOptions.RemoveEmptyEntries);

                if (_ops[1] != op)
                    return "";

                string name, value;
                name = sExp.Split(new string[] { op }, StringSplitOptions.RemoveEmptyEntries)[0];
                value = sExp.Split(new string[] { op }, StringSplitOptions.RemoveEmptyEntries)[1];
                value = value.Trim();
                name = name.Trim();

                while (true)
                {

                    if (sExp.Contains(op) && op == "<=")
                    {
                        _op = "Leq";
                        break;
                    }

                    if (sExp.Contains(op) && op == ">=")
                    {
                        _op = "Geq";
                        break;
                    }

                    if (sExp.Contains(op) && op == "=")
                    {
                        _op = "Eq";
                        break;
                    }

                    if (sExp.Contains(op) && op == "<>")
                    {
                        _op = "Eq";
                        break;
                    }

                    if (sExp.Contains(op) && op == "<>" && sExp.Contains("null"))
                    {
                        _op = "IsNotNull";
                        break;
                    }

                    if (sExp.Contains(op) && op == "is not null")
                    {
                        _op = "IsNotNull";
                        break;
                    }

                    if (sExp.Contains(op) && op == "is null")
                    {
                        _op = "IsNull";
                        break;
                    }

                    if (sExp.Contains(op) && op == "<")
                    {
                        _op = "Lt";
                        break;
                    }

                    if (sExp.Contains(op) && op == ">")
                    {
                        _op = "Gt";
                        break;
                    }
                    break;
                }
                if (!string.IsNullOrEmpty(_op) && !string.IsNullOrEmpty(name))
                    ret += string.Format("<{0}><FieldRef Name=\"{1}\" /><Value Type=\"Text\">{2}</Value></{0}>\n", _op, name, value);
            }
            catch (Exception ex)
            {
                Log("CSqlToCAML.propExp(" + sExp + ") error: " + ex.Message);
            }
            return ret;
        }


        private static void Log(string text)
        {
            //MessageBox.Show(text);
            LOG += string.Format("[{0} - {1};\n]", DateTime.Now, text);
        }


        public static string LOG;
    }
}
