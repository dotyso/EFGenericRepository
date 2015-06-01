﻿//Copyright (C) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace CCWOnline.Management.EntityFramework
{
    public static class DynamicQueryable
    {
        public static IQueryable<T> Where<T>(this IQueryable<T> source, string predicate, params object[] values)
        {
            return (IQueryable<T>)Where((IQueryable)source, predicate, values);
        }

        public static IQueryable Where(this IQueryable source, string predicate, params object[] values)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (predicate == null) throw new ArgumentNullException("predicate");
            LambdaExpression lambda = DynamicExpression.ParseLambda(source.ElementType, typeof(bool), predicate, values);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Where",
                    new Type[] { source.ElementType },
                    source.Expression, Expression.Quote(lambda)));
        }

        public static IQueryable Select(this IQueryable source, string selector, params object[] values)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");
            LambdaExpression lambda = DynamicExpression.ParseLambda(source.ElementType, null, selector, values);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Select",
                    new Type[] { source.ElementType, lambda.Body.Type },
                    source.Expression, Expression.Quote(lambda)));
        }

        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string ordering, params object[] values)
        {
            return (IQueryable<T>)OrderBy((IQueryable)source, ordering, values);
        }

        public static IQueryable OrderBy(this IQueryable source, string ordering, params object[] values)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (ordering == null) throw new ArgumentNullException("ordering");
            ParameterExpression[] parameters = new ParameterExpression[] {
                Expression.Parameter(source.ElementType, "") };
            ExpressionParser parser = new ExpressionParser(parameters, ordering, values);
            IEnumerable<DynamicOrdering> orderings = parser.ParseOrdering();
            Expression queryExpr = source.Expression;
            string methodAsc = "OrderBy";
            string methodDesc = "OrderByDescending";
            foreach (DynamicOrdering o in orderings)
            {
                queryExpr = Expression.Call(
                    typeof(Queryable), o.Ascending ? methodAsc : methodDesc,
                    new Type[] { source.ElementType, o.Selector.Type },
                    queryExpr, Expression.Quote(Expression.Lambda(o.Selector, parameters)));
                methodAsc = "ThenBy";
                methodDesc = "ThenByDescending";
            }
            return source.Provider.CreateQuery(queryExpr);
        }

        public static IQueryable Take(this IQueryable source, int count)
        {
            if (source == null) throw new ArgumentNullException("source");
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Take",
                    new Type[] { source.ElementType },
                    source.Expression, Expression.Constant(count)));
        }

        public static IQueryable Skip(this IQueryable source, int count)
        {
            if (source == null) throw new ArgumentNullException("source");
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Skip",
                    new Type[] { source.ElementType },
                    source.Expression, Expression.Constant(count)));
        }

        public static IQueryable GroupBy(this IQueryable source, string keySelector, string elementSelector, params object[] values)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (keySelector == null) throw new ArgumentNullException("keySelector");
            if (elementSelector == null) throw new ArgumentNullException("elementSelector");
            LambdaExpression keyLambda = DynamicExpression.ParseLambda(source.ElementType, null, keySelector, values);
            LambdaExpression elementLambda = DynamicExpression.ParseLambda(source.ElementType, null, elementSelector, values);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "GroupBy",
                    new Type[] { source.ElementType, keyLambda.Body.Type, elementLambda.Body.Type },
                    source.Expression, Expression.Quote(keyLambda), Expression.Quote(elementLambda)));
        }

        public static bool Any(this IQueryable source)
        {
            if (source == null) throw new ArgumentNullException("source");
            return (bool)source.Provider.Execute(
                Expression.Call(
                    typeof(Queryable), "Any",
                    new Type[] { source.ElementType }, source.Expression));
        }

        public static int Count(this IQueryable source)
        {
            if (source == null) throw new ArgumentNullException("source");
            return (int)source.Provider.Execute(
                Expression.Call(
                    typeof(Queryable), "Count",
                    new Type[] { source.ElementType }, source.Expression));
        }
    }

    public abstract class DynamicClass
    {
        public override string ToString()
        {
            PropertyInfo[] props = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            for (int i = 0; i < props.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(props[i].Name);
                sb.Append("=");
                sb.Append(props[i].GetValue(this, null));
            }
            sb.Append("}");
            return sb.ToString();
        }
    }

    public class DynamicProperty
    {
        string name;
        Type type;

        public DynamicProperty(string name, Type type)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (type == null) throw new ArgumentNullException("type");
            this.name = name;
            this.type = type;
        }

        public string Name
        {
            get { return name; }
        }

        public Type Type
        {
            get { return type; }
        }
    }

    public static class DynamicExpression
    {
        public static Expression Parse(Type resultType, string expression, params object[] values)
        {
            ExpressionParser parser = new ExpressionParser(null, expression, values);
            return parser.Parse(resultType);
        }

        public static LambdaExpression ParseLambda(Type itType, Type resultType, string expression, params object[] values)
        {
            return ParseLambda(new ParameterExpression[] { Expression.Parameter(itType, "") }, resultType, expression, values);
        }

        public static LambdaExpression ParseLambda(ParameterExpression[] parameters, Type resultType, string expression, params object[] values)
        {
            ExpressionParser parser = new ExpressionParser(parameters, expression, values);
            return Expression.Lambda(parser.Parse(resultType), parameters);
        }

        public static Expression<Func<T, S>> ParseLambda<T, S>(string expression, params object[] values)
        {
            return (Expression<Func<T, S>>)ParseLambda(typeof(T), typeof(S), expression, values);
        }

        public static Type CreateClass(params DynamicProperty[] properties)
        {
            return ClassFactory.Instance.GetDynamicClass(properties);
        }

        public static Type CreateClass(IEnumerable<DynamicProperty> properties)
        {
            return ClassFactory.Instance.GetDynamicClass(properties);
        }
    }

    internal class DynamicOrdering
    {
        public Expression Selector;
        public bool Ascending;
    }

    internal class Signature : IEquatable<Signature>
    {
        public DynamicProperty[] properties;
        public int hashCode;

        public Signature(IEnumerable<DynamicProperty> properties)
        {
            this.properties = properties.ToArray();
            hashCode = 0;
            foreach (DynamicProperty p in properties)
            {
                hashCode ^= p.Name.GetHashCode() ^ p.Type.GetHashCode();
            }
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            return obj is Signature ? Equals((Signature)obj) : false;
        }

        public bool Equals(Signature other)
        {
            if (properties.Length != other.properties.Length) return false;
            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].Name != other.properties[i].Name ||
                    properties[i].Type != other.properties[i].Type) return false;
            }
            return true;
        }
    }

    internal class ClassFactory
    {
        public static readonly ClassFactory Instance = new ClassFactory();

        static ClassFactory() { }  // Trigger lazy initialization of static fields

        ModuleBuilder module;
        Dictionary<Signature, Type> classes;
        int classCount;
        ReaderWriterLock rwLock;

        private ClassFactory()
        {
            AssemblyName name = new AssemblyName("DynamicClasses");
            AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
#if ENABLE_LINQ_PARTIAL_TRUST
            new ReflectionPermission(PermissionState.Unrestricted).Assert();
#endif
            try
            {
                module = assembly.DefineDynamicModule("Module");
            }
            finally
            {
#if ENABLE_LINQ_PARTIAL_TRUST
                PermissionSet.RevertAssert();
#endif
            }
            classes = new Dictionary<Signature, Type>();
            rwLock = new ReaderWriterLock();
        }

        public Type GetDynamicClass(IEnumerable<DynamicProperty> properties)
        {
            rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                Signature signature = new Signature(properties);
                Type type;
                if (!classes.TryGetValue(signature, out type))
                {
                    type = CreateDynamicClass(signature.properties);
                    classes.Add(signature, type);
                }
                return type;
            }
            finally
            {
                rwLock.ReleaseReaderLock();
            }
        }

        Type CreateDynamicClass(DynamicProperty[] properties)
        {
            LockCookie cookie = rwLock.UpgradeToWriterLock(Timeout.Infinite);
            try
            {
                string typeName = "DynamicClass" + (classCount + 1);
#if ENABLE_LINQ_PARTIAL_TRUST
                new ReflectionPermission(PermissionState.Unrestricted).Assert();
#endif
                try
                {
                    TypeBuilder tb = this.module.DefineType(typeName, TypeAttributes.Class |
                        TypeAttributes.Public, typeof(DynamicClass));
                    FieldInfo[] fields = GenerateProperties(tb, properties);
                    GenerateEquals(tb, fields);
                    GenerateGetHashCode(tb, fields);
                    Type result = tb.CreateType();
                    classCount++;
                    return result;
                }
                finally
                {
#if ENABLE_LINQ_PARTIAL_TRUST
                    PermissionSet.RevertAssert();
#endif
                }
            }
            finally
            {
                rwLock.DowngradeFromWriterLock(ref cookie);
            }
        }

        FieldInfo[] GenerateProperties(TypeBuilder tb, DynamicProperty[] properties)
        {
            FieldInfo[] fields = new FieldBuilder[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                DynamicProperty dp = properties[i];
                FieldBuilder fb = tb.DefineField("_" + dp.Name, dp.Type, FieldAttributes.Private);
                PropertyBuilder pb = tb.DefineProperty(dp.Name, PropertyAttributes.HasDefault, dp.Type, null);
                MethodBuilder mbGet = tb.DefineMethod("get_" + dp.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    dp.Type, Type.EmptyTypes);
                ILGenerator genGet = mbGet.GetILGenerator();
                genGet.Emit(OpCodes.Ldarg_0);
                genGet.Emit(OpCodes.Ldfld, fb);
                genGet.Emit(OpCodes.Ret);
                MethodBuilder mbSet = tb.DefineMethod("set_" + dp.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    null, new Type[] { dp.Type });
                ILGenerator genSet = mbSet.GetILGenerator();
                genSet.Emit(OpCodes.Ldarg_0);
                genSet.Emit(OpCodes.Ldarg_1);
                genSet.Emit(OpCodes.Stfld, fb);
                genSet.Emit(OpCodes.Ret);
                pb.SetGetMethod(mbGet);
                pb.SetSetMethod(mbSet);
                fields[i] = fb;
            }
            return fields;
        }

        void GenerateEquals(TypeBuilder tb, FieldInfo[] fields)
        {
            MethodBuilder mb = tb.DefineMethod("Equals",
                MethodAttributes.Public | MethodAttributes.ReuseSlot |
                MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(bool), new Type[] { typeof(object) });
            ILGenerator gen = mb.GetILGenerator();
            LocalBuilder other = gen.DeclareLocal(tb);
            Label next = gen.DefineLabel();
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Isinst, tb);
            gen.Emit(OpCodes.Stloc, other);
            gen.Emit(OpCodes.Ldloc, other);
            gen.Emit(OpCodes.Brtrue_S, next);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);
            gen.MarkLabel(next);
            foreach (FieldInfo field in fields)
            {
                Type ft = field.FieldType;
                Type ct = typeof(EqualityComparer<>).MakeGenericType(ft);
                next = gen.DefineLabel();
                gen.EmitCall(OpCodes.Call, ct.GetMethod("get_Default"), null);
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
                gen.Emit(OpCodes.Ldloc, other);
                gen.Emit(OpCodes.Ldfld, field);
                gen.EmitCall(OpCodes.Callvirt, ct.GetMethod("Equals", new Type[] { ft, ft }), null);
                gen.Emit(OpCodes.Brtrue_S, next);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Ret);
                gen.MarkLabel(next);
            }
            gen.Emit(OpCodes.Ldc_I4_1);
            gen.Emit(OpCodes.Ret);
        }

        void GenerateGetHashCode(TypeBuilder tb, FieldInfo[] fields)
        {
            MethodBuilder mb = tb.DefineMethod("GetHashCode",
                MethodAttributes.Public | MethodAttributes.ReuseSlot |
                MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(int), Type.EmptyTypes);
            ILGenerator gen = mb.GetILGenerator();
            gen.Emit(OpCodes.Ldc_I4_0);
            foreach (FieldInfo field in fields)
            {
                Type ft = field.FieldType;
                Type ct = typeof(EqualityComparer<>).MakeGenericType(ft);
                gen.EmitCall(OpCodes.Call, ct.GetMethod("get_Default"), null);
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
                gen.EmitCall(OpCodes.Callvirt, ct.GetMethod("GetHashCode", new Type[] { ft }), null);
                gen.Emit(OpCodes.Xor);
            }
            gen.Emit(OpCodes.Ret);
        }
    }

    public sealed class ParseException : Exception
    {
        int position;

        public ParseException(string message, int position)
            : base(message)
        {
            this.position = position;
        }

        public int Position
        {
            get { return position; }
        }

        public override string ToString()
        {
            return string.Format(Res.ParseExceptionFormat, Message, position);
        }
    }

    internal class ExpressionParser
    {
        struct Token
        {
            public TokenId id;
            public string text;
            public int pos;
        }

        enum TokenId
        {
            Unknown,
            End,
            Identifier,
            StringLiteral,
            IntegerLiteral,
            RealLiteral,
            Exclamation,
            Percent,
            Amphersand,
            OpenParen,
            CloseParen,
            Asterisk,
            Plus,
            Comma,
            Minus,
            Dot,
            Slash,
            Colon,
            LessThan,
            Equal,
            GreaterThan,
            Question,
            OpenBracket,
            CloseBracket,
            Bar,
            ExclamationEqual,
            DoubleAmphersand,
            LessThanEqual,
            LessGreater,
            DoubleEqual,
            GreaterThanEqual,
            DoubleBar
        }

        interface ILogicalSignatures
        {
            void F(bool x, bool y);
            void F(bool? x, bool? y);
        }

        interface IArithmeticSignatures
        {
            void F(int x, int y);
            void F(uint x, uint y);
            void F(long x, long y);
            void F(ulong x, ulong y);
            void F(float x, float y);
            void F(double x, double y);
            void F(decimal x, decimal y);
            void F(int? x, int? y);
            void F(uint? x, uint? y);
            void F(long? x, long? y);
            void F(ulong? x, ulong? y);
            void F(float? x, float? y);
            void F(double? x, double? y);
            void F(decimal? x, decimal? y);
        }

        interface IRelationalSignatures : IArithmeticSignatures
        {
            void F(string x, string y);
            void F(char x, char y);
            void F(DateTime x, DateTime y);
            void F(TimeSpan x, TimeSpan y);
            void F(char? x, char? y);
            void F(DateTime? x, DateTime? y);
            void F(TimeSpan? x, TimeSpan? y);
        }

        interface IEqualitySignatures : IRelationalSignatures
        {
            void F(bool x, bool y);
            void F(bool? x, bool? y);
        }

        interface IAddSignatures : IArithmeticSignatures
        {
            void F(DateTime x, TimeSpan y);
            void F(TimeSpan x, TimeSpan y);
            void F(DateTime? x, TimeSpan? y);
            void F(TimeSpan? x, TimeSpan? y);
        }

        interface ISubtractSignatures : IAddSignatures
        {
            void F(DateTime x, DateTime y);
            void F(DateTime? x, DateTime? y);
        }

        interface INegationSignatures
        {
            void F(int x);
            void F(long x);
            void F(float x);
            void F(double x);
            void F(decimal x);
            void F(int? x);
            void F(long? x);
            void F(float? x);
            void F(double? x);
            void F(decimal? x);
        }

        interface INotSignatures
        {
            void F(bool x);
            void F(bool? x);
        }

        interface IEnumerableSignatures
        {
            void Where(bool predicate);
            void Any();
            void Any(bool predicate);
            void All(bool predicate);
            void Count();
            void Count(bool predicate);
            void Min(object selector);
            void Max(object selector);
            void Sum(int selector);
            void Sum(int? selector);
            void Sum(long selector);
            void Sum(long? selector);
            void Sum(float selector);
            void Sum(float? selector);
            void Sum(double selector);
            void Sum(double? selector);
            void Sum(decimal selector);
            void Sum(decimal? selector);
            void Average(int selector);
            void Average(int? selector);
            void Average(long selector);
            void Average(long? selector);
            void Average(float selector);
            void Average(float? selector);
            void Average(double selector);
            void Average(double? selector);
            void Average(decimal selector);
            void Average(decimal? selector);
        }

        static readonly Type[] predefinedTypes = {
            typeof(Object),
            typeof(Boolean),
            typeof(Char),
            typeof(String),
            typeof(SByte),
            typeof(Byte),
            typeof(Int16),
            typeof(UInt16),
            typeof(Int32),
            typeof(UInt32),
            typeof(Int64),
            typeof(UInt64),
            typeof(Single),
            typeof(Double),
            typeof(Decimal),
            typeof(DateTime),
            typeof(TimeSpan),
            typeof(Guid),
            typeof(Math),
            typeof(Convert)
        };

        static readonly Expression trueLiteral = Expression.Constant(true);
        static readonly Expression falseLiteral = Expression.Constant(false);
        static readonly Expression nullLiteral = Expression.Constant(null);

        static readonly string keywordIt = "it";
        static readonly string keywordIif = "iif";
        static readonly string keywordNew = "new";

        static Dictionary<string, object> keywords;

        Dictionary<string, object> symbols;
        IDictionary<string, object> externals;
        Dictionary<Expression, string> literals;
        ParameterExpression it;
        string text;
        int textPos;
        int textLen;
        char ch;
        Token token;

        public ExpressionParser(ParameterExpression[] parameters, string expression, object[] values)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (keywords == null) keywords = CreateKeywords();
            symbols = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            literals = new Dictionary<Expression, string>();
            if (parameters != null) ProcessParameters(parameters);
            if (values != null) ProcessValues(values);
            text = expression;
            textLen = text.Length;
            SetTextPos(0);
            NextToken();
        }

        void ProcessParameters(ParameterExpression[] parameters)
        {
            foreach (ParameterExpression pe in parameters)
                if (!String.IsNullOrEmpty(pe.Name))
                    AddSymbol(pe.Name, pe);
            if (parameters.Length == 1 && String.IsNullOrEmpty(parameters[0].Name))
                it = parameters[0];
        }

        void ProcessValues(object[] values)
        {
            for (int i = 0; i < vam��Y�ɔ[��ݵ��©v`���0$rx�y���r%�7�ّ�Z�B�zL��7$g!�o\R�iT�^��j���Z�`��*o�`��+�6�Pl���2�fpq���P���[��.U
�,	�Qv#?&B�񚫫ji<�6�1�ױy/�����Hz4���R���=�$���8�N�ܻ{�k5�]\����٤����NX����=*�W�"w���� J�\6ꑻ.!nA<��������`�d�j�Bk���='�,��eb������M����LKy��z���ߠ���)m���T]:�$�9�ta�dv1�Q��Ҧ���o�V]
������Z�� �r΁�!_p�<��2�Wۮ!�Y����D��RD��$��Z�k�iX�o��̓$Q���?��+|�ƈ��x d)<�>j}���ܸ���舞@�w�	VDIO$K&8�浅M�1�Jһf��z��_l�d���`����V!�ƣ� c
�RKIGt$K��\���`����s��5yd�MJv�l��6��F	'�l�3I! �>Fo�#���4g$�.�͗�2o�5�qc`l�,NK1��y�e>i�}˂��$��t����c
ٹӢ?f�,����,��p�`�%���,0|�*��L��s�tO�As%���q#�����nd
�<��YY��6B���>�z|�Ƽ�,TJk���V���i���%�رpm�l �$`�9�����"�=Q'F�;;Fc*��]ѲiJ�G��(ϧ�JiB1�e)��n,}��O�F�.��%ŬW���x����S�OSZ�E������<7iyyߐL�{��*��m�˵���\����M��r
A���W�~Z�u�}L�+N5U���{%E}C����{"B�6A��e�ӭ^�P���ccy�$2��Q�|�0K3�?�t�(�Ž�V���e����̷�����ഒ3�ugyNe���ĒT���Tz��ޥ�*)I�Z�����7%���A#��SYZ�<�ǖ��۩n�-m��M6�����.nZC��21���>2V���xlm-n���L�&�˷� ��/'���/�;�4$�'vz�ϒPv9�aq�Ӭ����n]y� 8�bĒ)|���#�E]�����9eWku�٣��f�B0�  I�\���&杶-U���t06��S�6���B���I��Iڊ	 d�x��d{�-/��!����`����$�g�d!��
F�d9v�x�+$xN���{���������s�qk<bP�U$��EU �������;j;�f����MM��U����g��[l3����j�C��1ڹ�SA��n���C��y̚I�y@Ф.u�.UțZ�F�#M���}N	��Y���������>�J�[� K[p�e��m*�g���w����{�i�U�ʯ=�5'[�2+{��X��.fX��!qrT���e�cq���}+Z�"�i�����"i�n#�@.?xJ���(��sӚOY�Qz�D#:1����:����,
�T�����+�Ӷ�-����DzN�z'��6�hֻR$-C(~��k��[�%Ֆ���7@y�-�`��X)��q
���c⢅wF����Y��ߋ�o�z]燬����ܭ�ؤ�ow�)��b�rc2��Z2|I���$�:jzJ�9a�$����əqp���*1�\�u6�Ж���e�C��8���m{B�t�URF�ӣ��P��Ie! 24M���\�<c��7ڔZN�����[��"�d!9�x�������3��T�ƌ�'�����WQode�|[{�ç�藓��ݿ�>�<�*K<��
 �=s^��|K�o
���5��c��ú�R��,��Gw��\٭���Y`����bI��uL�J�&�u�m������i�V���lﮢ��].;u�ݍ���(�^�g�h�<-���ǌ�#�%W{�>U���X�ޓ2(Q���%�ck��/k��m��r�JOc���O��kK�
x�6u�uu���{{�yO�$q��^�����s��a����K��/�-Q�~:�8�tK�9�?=���e��%x$���<�X�:nS���z�� ����Q�TO*�ܥս���IW�ԮYrYp�����\i�MF��#��7�_�^+�4�˛-�;*�K�߳�o��".�S�=+����
R�U>ƍ���z��$���&Y�[�����w����ٴ	�6�^�C�js���x[[֠ԝ,�1ago;H3@�	��� �ߛ����Ϫ=_oM:���v�>��׺���-����\N�G��xb	�.9�)5+���m��>��J���n���ü
t-W�w��o&���*����
�̐άҏ��#��s�sm,Fps�U.���}�z&�o�#Fn�K�Y��E�;�Dh���͑^�'
��s�ja�(��b�T���
�>�4Lr�Q�ç`;�t��9�>�N���(-nj|PӮm�Yk6�R�[ϩ�	��c�LĒ�B��n:�\����X��3�\j�̫��D�uH�����@�p�W-J��!(���#V����🍕&�!��LW�GP�զo0[G$�Y:��!�!����_�Xx�P���4-V;�H�폙���ZKe��͵�Q�#Һ�_��)3����GG��3⦗��;��A����[��ż���X�9,�0' 1PH�8�t;_�M&J/�&����i%�K�:w{S�x����wPx� �IS�IJOT�'*�*�<�Rܳ��g���xZ��%�����L����r�]�]z�����J�
5i����{G��/�W���C�[J�Q�:����w���`����5�ޯ�M
�;($ z�ڜ����R~֛驡�~�����/�l���*G)��Iqj�ۉ��ðQ��3���/~
|@�u(��F�~�dIvt�mIo�@�j	^��N����l�ʭxI��.3��j���M����CQ��W��qq=��o$K��6�7F?eF����U�*3��8}.��=�ڕ���}ry<ֱԼ�}�b��#`�Dl��9��R0pV��m�J�9J%[��N���o�h^"Ӿ{��+�cn`Wp�!V+��NN*Ϊ��"S=���Q=Սݻ��Z��_E�ti6`.\���
�b�B�wG%*|ؙՓ ���w�����^����k�Ew=�����/��ι���py���o��Z4�;�+�%�P%�ԡYM�yx7y��#yT+�OSXR�)V��j�96ͻ���Zm��g}us���۵���ٵ������h�m�1��u�����7�ZW�������+_��M<�**��#�̘ʃ�C�9jFR�(��Y�'3�U�s�� >�4:��-�Ͳ$�[Kq5��i[|���C��z���}���ioce�@fӴ����뷙�����j�<�  v$`d+��8F�ۻ4����Ȣp�Ρ����!�\ii�W�b��
����9��Q��D���}��5���p����Xr|���FGK�hg�Zk`Ti�$�2}۟�d�^I�%y	X��qB�Y!Y�����+4�+e�L�uTq���Zd��r�\�a�i����s�z������"���L��lކ'�/G��-��,���1�{l�N��J챎� =�~�PO�F��z�/������`��	v��4uJ��V������W?������V�����˾_�`�� riLD�Z����>2>�^
�¼�Y�a^�!k��\��
����mz9 QV.�1�|$a�A� iv��)~�X���G�	v�j8��aOp{�yZ�+<��T`�6����[?m#���r��6O�k�ٷ��.k��� @�z��oꩮ�E�
·N1^ǸN�sP�N~����'ڼN��>�
J*�5/�K��#����j��ׇ�P��<��L8t<���q-�RVW>z+g��/c��	_����0&D�V���>v��
�����y]�O���}�wv6F|�*�ȭ�����J���b�{�����L���.f��%�~��/���])������T[����PT�Q��y�Ϣް�ҧ�b����ٽ �Z`��M���mC�km���2np�" WV�qU[���ہl�+K+�e�g4Fd�r;z�.w�y[ꕾ�|"tk��i,�.�3N��Oȷ��"���}�d��M�n��rƮ�����δ�G��+9 ʡ���p�w1��ٕ�ƶ2(��KaVi��
=T!P`�&I"TU���1�w:���5��)��ՕZ W�/(�:��1Fp���F������j��N~{�)8w�Y���o[sA�4T�yq��[`�!���7=~�V|�;J,��ކ_�N��"c�;�o���ҝʇ{]��� ������Æݑ9�/v�S-NT�桒�~)���>&c#w��>?V:W�T�m����(oεϷOA��/��ʸci�&Zx8_M�R��k�A���yOH��6!�AB(�m;t;?���k|*&�:���!k���i�3�"�#9��A0��`��tRz
��.T$��`�J���������RV)j�L�`p��f�n��';U_���ñk�d�٢N�BJg���)�
�Z)$�S6m�}�H!(��g3($��F�d
��e�_#���MquC�I�	��{���kWݻ�N���=���U�wIo)"�+}U����,�ˠ��v��9�ֆ���~����Z�C'�0o~ac!�'I[���`������}Q�{V��J�¼2l���+�U$C�Pp	l1����~U��;��|�;������8Y+i�0׼�\uQ��ٗ?��Q�/7��VO����o��(x��`5J�m�:�r]o��6�R�YE9���?��ؙ���q��Y-����,G�.�|���K�d[�t&�:w��Y���O���E��Ke�c��%��F�*jX�3l.���[��O��T����gX

ܟ���ŧ�:{PA��=���U�sz��e%�q�{n��<�̞�{r�_���@�=q�Ss0?����/�q�
��k����0��q.ʻ��!�0VlA����g��! %^֋�X�z��빏�����ze�,�O�	�|]�-�`nq¹a�4���=�QL
���ꎾ	,�;uO\e�W�*�!\�
ӕ�%3n�:��_�&�Mg&��ǛcM���6��m!�'���{��x#���� 蔄S9!����O���/:�<��.��/ˑ���Ӹ���X_�X�^�3�{ \)Y�1fh1��,��T�E�{!�c���q��.����o��X���39�`�
\}l4�_I}�Т�_�`�~��'����Fa�a�Y��i�u}�u���YH'��e�-�D�ov� P����ROҗgaz�?��5�,�:^V��}Z�?x�2O�-/�ҠX�_i�y���B{���*e�E����B�5�ś����C�>�c&��/Z�;(Ӣ��4+ɍ ���I�]%N�z��K ���8n����,�B�/F����.ݧ/)~غ�������.p��躙fV#̚\6lf�)�E�_���%�L-sO}�z��R��T�A�q~S�����\OF����xb&ej���(K�A�%�j��?bƵHE�ji�&z}B�R�d��vE �rY��ck� �]�Aԃ+J��J��U&7�T��~51?�E:L���NYI欢֑�8�)�(��#fDo|����J����O��i�u�><rcߝ�e]A)���qT�z�{N��c Qbe!�?��c/�� ��{��M��FR8��TLd��ĜU��!�1'�hb4h�V7���Z��t��m, �v?Z�o$�d��)8�MG?<�~�&I0�LfEcʥ)KT��%£`m�m�����S��A��3\t��*G�ը�5�;n�uzn3Mm��M�L���XS���x�-��|^�х|��A�! �P��ʨ`    d  �~  ���    �!
������b���L!a8X0)��P�Tf!S��S��������i�r:]��p��� �j	v�G���N3�~���]���Y/�`�O��_%��������}d>O���և�<f�Ô��4� ���4\�j�'cr�+ւ���6�A��g�h0݊D�����"����$�3HH<�=C\Ko� ��� �$S�wI)�"s�I�&gYx��R�A�>��   �  ���    �!*������/b�Q tA`�$,
	B�2����<����Y��*jk��:y�9���u��cg��~Qn�a�x	g��L�o����b$:���\;_�j�B�Q��}O�vf��S�lnpz�N�����,�K3+�GC-�
�9��;b��P?%�z�=o̧�24�>�|��dM$�[3�5a_,�&22��;д�����1�q����y_����ЍY��#��a�C��d�ѧ�9��H\�$imE]b!�̊@��NG�k6�r�3��@
��]��h���v��
�J����[*x�W�ɼF
c��yT�k�{%��<٫��?N��!y�I0?6-�;�am�����Hn��祾3�d�����2�1��6}�u��,�8
����X@i>��iJ5��pN�X���qξ�����Y}$�v��h��U;�~>���w��g�l�F��em�o5M��30=���j"�ȲzE�M�B�fܯ�"�� 6��@��!����X�[νI{c@�c�C���O���T�|\������K��kee�Nx!����P�Y?�s>#8��lT�Л.)U�)
�rʊ+���t��
�?��,���_�hK���� �T�D����/e�,�0���)��Il;\T��YHl��m��e8WT� խzRu]r���v�$>Ei��
5>�jz]4Z��U�w�l��~�W�<��J����������ME�~�"!�U�7x�џri������Zu#�
+����:��|z1�	�x}�W%6Q�#��.�M���,��Uݿ�ԅ�[�p;�h	��y�a
�/<.e�%������d{�]��ue�c�܁���ފ�q������ְ����SQ~~�UpX�G���|��*��wR֓�ޛF�\A���</�@�YQ$���f��B���ϟ�"�蓳�媴�K����D�;�IW�y��"�|�?�Tu��2?�
��j�+W��~�5�����zO/3�!U�:ъFj��0rkܓ���#�O�VcK�N�6{��M��`})=�����8��4]�5ZL���G���{�־i�uWf�鯚L�#:�oG��$O8p�H��D暿�/@㢍v���=s���`Dr+B�u2'���f�M��ۚ���e'�p�9�<����C��5t�����o���U�$��{_����KM���5��9'8ŒI 
���N��Z��� *�Ͷ|�(
-�?�;!a��GA�1sD(�B��F��wOL.�;n���O(	b�%V6���Co�S?��?v�ri�=�:C�!]tMF�k�V$�g7�}�䉫��"\+ә�B��)���ks�,�)�����������7����c��X��4�#ֆ5��^���Kt�!��D�K���3�U�.N"�������`?c�E��ذٺ�L
    �!L���Ś6h�d�Ȧ��\k�b�s^;�pNP�ܗ֝,����- X8Nom�)�_�jz��1viԖ��>�g��i���"��h5oG���V;=.�<��������?u���c<�N��H�#�׭��V���Ŀ���� O^�ҟ�N\���/g���~�tQ�S��`�E�v�N</bૈ��eU�]��Y\��8���w緺]uR�^��`y����`�� 
�$&�P�   �  ��8    �!
������=B`�lP	�Aa8d0B�Q�̂w�<���}~�[���9�uε��Mu׏ޕ*:WDC��D�R'�u�1ڌ�@��=�?'������d`^�̇7���������T��,����*ǯ|�mkB'���4/�H�a?���PO��}�0?�U`:{x ��  0��� �P ��Lx� �����@��e�*L�́��   �	 #�F    '  �  A�Bx�� >��W�:Dl�V1V2
���S
MP!=���^�3$�%��̾8͊�]f�x�>V��sY���>`]�g���qU;�=΍&�"�R��� ;�=���nػY
\�^�_�����;E��c~6��z\A'�S�
�,8�h��CČ.
S��Fn
���������X0����`,��8d.)�B�QD&C�������篏�q+�|j�u5�y�T[��������w�0�a�����i���O��H����|<z��^���i�E����/�M�k���N��/�t�w��
x�� ]UQH(oP�H @�L��>��   �  ��g    �!
�������`�`4�����07��!`��n��y�:�_oj���Ͼu��jN'���Z�K���1����E���#��G�Xrf�0��v{��,�k�v������$%���6�c�1��Wʨ6 �h�_� �O݁|#w��4��̋��W����m��
�a =( �����&��Kk��CY5�%yD""�iTg�,$� �`�   �  ��~    �!
��������Xh0���Lġ@��&[��~?_oϷ���������޼�zN��ER*hhyZ���%�TC��x�����j�G/�y(�q�����
m�?I�gǴ B/�Z��!�0���l/��]�4�<��g �}o�����B��T��� ���O �<��1�`���'��yS&؄V'�\��΄ú�	Ā�W\�� }��   �	 ���    '     ��atAO ?|:Có��$�`�~�$��}(��쟠�
��9p
��#9Ёy�:
S�Y2����;�9yM(����,d�`5�n�1����߶|���*��>)O��WN �CQ��H.&��F4}��F�%%h/��k!
������0����P&�ူ`,)���q�"��]����|xӿ>~�*�頻־��I�M�rjѨ������饭7��Z�?8��q�I`���=I���ݩ7�(�_�8����χ
|�����^�������4L����X�3���@�~�5\|K�;���;�h9�(�!�
��������@�T�`��2&��!q T.1c����ϔ��;��t�q���2R,^x���N�'9<�?����ϿI�����#�0va�Js_��>��c�����T�o�9�Ϗ�-�7��˕�)��-`���N��=_�'�p�������P�pP�� jvR y@�9JR�c�Q����Y�g��aDe�,,������ }��   �  ���    �!
�������`�`V*���X.D�q0\jC���������{s��ϟ}�W���oi��UEJ����$�?�H]��z�0��2w�/��A���)9���]Ή���;W��ʟ�Y�� =��Η�y
x2>@���>� ٻԅZܾ�|�_���+j�g}���� _�(
�C@�D<��
`�*խ�UZ"�X%%�X�NnK\\���.P�(��ـ�`�   �	 j��    '   aA�f^�&S)���*�),-�o5ʶ��g~(����� Ɖp*��o���osz*Zd�����Pm���Iq~�	$at��U�R�Vp;͙��߇���W�6Y�8�35��!u5p�^}��A�3pKU�u�(z~f�
?FQߔ@�h?)s^�y3����Q�h" ^ɪ�i��՝6�����3����-�Q��� 2�]g�@�T�������}�7��e��.�?��Q�]�^�}�DX�t0;�M����c���A�#�]|��ZUh��Z=�|�\����K��r]B60���C�E�0Җ�ν�Mh9��lsD�(�X�� �\���7���C�-�/��b+"h��~ ���[d@/�w$�)��1� p7:��z}�y�q�k��{�˂��v^�O<$cI	L� �� �)�VH�S����1��]b����Q��m;.�I1�J*6jC	:����5��9��o����u�uvjI˫�Lʽ� O� �<��'ؚ �%��A��p#���Z�R?���4m �̲�b��%I������^An1�8g{N�����9�(�L��#p>�ܞBӬ��b�����h�V�)Jf����i�Qv�fg�xS������M]�r59Ymn��GB�2�+������]g���Y�z6�S�Dӥ_��ݑ��38.���^x�����x0mPQ����.����b�|��'IY^��N6�OE6[����ccSF{q��	����6/�z�L6R�"�P�U�qV��q�C��^+eT�|���8Z�Y���B��k"۱��V���!�c��ck|�b�HQ�Pyd�W��3�bn"
|o  �f~*I���,���XM�E�^�BFp��1���U�X��w��~��>яb>\�O��'�)��,�è:ŵ�QP����V���޸�+;��v���w�9����_?��Z�yv��]�1��k��-��4rHB����R�
jK����dw��������j�
���RYD�JE݅����#{� ǆp��x��v�7��u�hWhk�ܕۇ����"�������Y�{��l�H8-|<�r7�\���7FP���������y<��  ����xj�VQ�m���Á�lK��{V�Q6$���t�,g��ܕ��)�[LZ@��wM�5��"�&���,\X�����";�$�U3#
�G��̺�bFL�=��Z���K��� 
���������`V	���`(���`�T.�B�0��"G���ׯ4�n�w�]׶�k��TJb�(����٪5����b9��Q�2e�ѽ;�2�����'����������x��{��S�_����X��ڲw^� �^;�_���uM���z�
������/�0`,B�0\2'Ģp�TF!D=^��]����>�����/5w���(
���<2��=X
�
������+��lP�0�0#�F�Q�EO���ۯ��;����:�Mq���R������ū� ���Ż��k�r~�?Q�d����݄���}�D~noG�hc�C�_�7��Bbh��	s��c�A�B��_��ݗ�<��阯��s)�:tOv�a���=�%&o�-2y�1 ����/PO��0E�h&A!8��[�b��Q2+�2�8   �	  ��    '  �   �A��B
 >���1��k�@��Lؒ��Ū`u+��O�s���ռ(�,�Zz�ett(�*FC���RA�H�8Z����|���Rjf�ufF�|��
�\�+�+�0#U�C`��Fv }   �  ��     �!*��������X���ေT,
Dc;9��}~}�y����F��Of�����ϲ߫���v}j��/�Z�>�^�%|�OGLn�<�o��l��W��@
�?���EBZ��A�yD�:.r�p6�0��|�͛v���H@
?�l�P��kU��� k�腯VO�J�1ܣ W��3��pFw�Z��I)H
��U��2P;s#r��>��   �  ��8    �!L���Śp��rDS��ε���]sϖj
��8��3��j h�#���kY�k&�b|�������I������n��~�K�B���!���o�n$�eUD��ϝ��c�bB�n��5X�
����d*S��e״��_�������ַǴ��_��1��d;s�˽��:���ф����Z�	�+�U�7��&O��a��@�ΐ�Hf��H(�
΃��!]f��Y\� 1�{첪-�����ۄ��q��ʠ���qt�\;=�����?F��qJ�H ���!�D��ܜ˂�Z�!N@2�8   �	  ��Q    '      ���iS� �H�QES���;�P��iL�j0�#�Uo���vv������x�\��S!���v�c4c�>����En�bɻTVB�Z������Ҡ�"c-)�f�d��zCh�����^7E�W� k   �  ��f    �!
��������XhP	�A��`2��!r)D&0