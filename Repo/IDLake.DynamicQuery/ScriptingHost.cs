using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;
using Roslyn.Scripting;
using Roslyn.Scripting.CSharp;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace IDLake.DynamicQuery
{
    /// <summary>
    /// Host class to execute our script
    ///http://www.amazedsaint.com/2012/09/roslyn-september-ctp-2012-overview-api.html
    /// </summary>
    public class ScriptingHost
    {
        private readonly Session _session;
        private ScriptEngine _engine;

        public ScriptingHost(dynamic context)
        {
            CreateEngine();
            //_engine.AddReference(typeof (Hub).Assembly);
            _engine.AddReference(typeof(DynamicObject).Assembly);
            _engine.AddReference(typeof(Binder).Assembly);
            _session = _engine.CreateSession(context);
        }

        public ScriptingHost()
        {
            CreateEngine();
            _session = _engine.CreateSession(this);
        }

        public IEnumerable Rows { get; set; }
        public object Context { get; set; }


        internal object GetValue(string key)
        {
            Func<object, object> getter = BuildDynamicGetter(Context.GetType(), key);
            return getter(Context);
        }

        private static Func<object, object> BuildDynamicGetter(Type targetType, String propertyName)
        {
            ParameterExpression rootParam = Expression.Parameter(typeof(object));
            CallSiteBinder propBinder = Binder.GetMember(CSharpBinderFlags.None, propertyName, targetType,new[]{
                                                                 CSharpArgumentInfo.Create(
                                                                     CSharpArgumentInfoFlags.None, null)
                                                             });
            DynamicExpression propGetExpression = Expression.Dynamic(propBinder, typeof(object),
                                                                     Expression.Convert(rootParam, targetType));
            Expression<Func<object, object>> getPropExpression =
                Expression.Lambda<Func<object, object>>(propGetExpression, rootParam);
            return getPropExpression.Compile();
        }


        private void CreateEngine()
        {
            //Create the script engine
            //Script engine constructor parameters go changed
            _engine = new ScriptEngine();
            //Let us use engine's Addreference for adding the required
            //assemblies
            new[]
                {
                    typeof (Type).Assembly,
                    typeof (ICollection).Assembly,
                    typeof (ListDictionary).Assembly,
                    typeof (Console).Assembly,
                    typeof (ScriptingHost).Assembly,
                    typeof (IEnumerable<>).Assembly,
                    typeof (IQueryable).Assembly,
                    //typeof (DbSet).Assembly,
                    GetType().Assembly
                }.ToList().ForEach(asm => _engine.AddReference(asm));

            new[]
                {
                    "System", "System.Linq",
                    "System.Collections",
                    "System.Data.Entity",
                    "System.Collections.Generic"
                }.ToList().ForEach(ns => _engine.ImportNamespace(ns));
        }

        public object Execute(string code)
        {
            return _session.Execute(code);
        }

        public void ImportNamespace(string ns)
        {
            _session.ImportNamespace(ns);
        }

        public void AddReference(Assembly asm)
        {

            _session.AddReference(asm);
        }
    }
}