using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
#if !NET20
using System.Linq;
#endif

namespace LpSolveDotNet
{
    internal static class Errors
    {
        public const string InequalityNotSupported = "Inequality is not supported by " + nameof(lpsolve_constr_types);
    }
    public struct LpVariable
    {
        public LpVariable(int columnNumber)
        {
            ColumnNumber = columnNumber;
        }

        public int ColumnNumber { get; }

        public static LpSum operator +(LpVariable left, LpVariable right)
            => new LpSum(left, right);

        public static LpSum operator +(LpVariable left, LpSummand right)
            => new LpSum(left, right);

        public static LpSum operator +(LpVariable left, LpSum right)
            => right.Add(left);

        public static LpSummand operator -(LpVariable variable)
            => new LpSummand(variable.ColumnNumber, -1);

        public static LpSum operator -(LpVariable left, LpVariable right)
            => new LpSum(left, -right);

        public static LpSum operator -(LpVariable left, LpSummand right)
            => new LpSum(left, -right);

        public static LpSum operator -(LpVariable left, LpSum right)
            => new LpSum(left).Subtract(right);

        public static LpSummand operator *(LpVariable variable, double factor)
            => new LpSummand(variable.ColumnNumber, factor);

        public static LpSummand operator *(double factor, LpVariable variable)
            => new LpSummand(variable.ColumnNumber, factor);

        public static LpSummandConstraint operator >=(LpVariable variable, double value)
            => new LpSummandConstraint(variable, lpsolve_constr_types.LE, value);

        public static LpSummandConstraint operator <=(LpVariable variable, double value)
            => new LpSummandConstraint(variable, lpsolve_constr_types.GE, value);

        public static LpSummandConstraint operator ==(LpVariable variable, double value)
            => new LpSummandConstraint(variable, lpsolve_constr_types.EQ, value);

        [Obsolete(Errors.InequalityNotSupported)]
        public static LpSummandConstraint operator !=(LpVariable variable, double value)
            => throw new InvalidOperationException(Errors.InequalityNotSupported);

        public static implicit operator LpSummand(LpVariable variable)
            => new LpSummand(variable.ColumnNumber, 1);
    }

    public readonly struct LpSummand
    {
        public LpSummand(int variableColumnNumber, double factor)
        {
            VariableColumnNumber = variableColumnNumber;
            Factor = factor;
        }

        public int VariableColumnNumber { get; }

        public double Factor { get; }

        public static LpSum operator +(LpSummand left, LpVariable right)
            => new LpSum(left, right);

        public static LpSum operator +(LpSummand left, LpSummand right)
            => new LpSum(left, right);

        public static LpSum operator +(LpSummand left, LpSum right)
            => right.Add(left);

        public static LpSummand operator -(LpSummand summand)
            => new LpSummand(summand.VariableColumnNumber, -summand.Factor);

        public static LpSum operator -(LpSummand left, LpVariable right)
            => new LpSum(left, -right);

        public static LpSum operator -(LpSummand left, LpSummand right)
            => new LpSum(left, -right);

        public static LpSum operator -(LpSummand left, LpSum right)
            => new LpSum(left).Subtract(right);

        public static LpSummand operator *(LpSummand summand, double factor)
            => new LpSummand(summand.VariableColumnNumber, factor);

        public static LpSummand operator *(double factor, LpSummand summand)
            => new LpSummand(summand.VariableColumnNumber, factor);

        public static LpSummandConstraint operator >=(LpSummand summand, double value)
            => new LpSummandConstraint(summand, lpsolve_constr_types.LE, value);

        public static LpSummandConstraint operator <=(LpSummand summand, double value)
            => new LpSummandConstraint(summand, lpsolve_constr_types.GE, value);

        public static LpSummandConstraint operator ==(LpSummand summand, double value)
            => new LpSummandConstraint(summand, lpsolve_constr_types.EQ, value);

        [Obsolete(Errors.InequalityNotSupported)]
        public static LpSummandConstraint operator !=(LpSummand summand, double value)
            => throw new InvalidOperationException(Errors.InequalityNotSupported);

    }

    public readonly struct LpSum
    {
#if NET20
        private static IEnumerable<LpSummand> EmptyEnumerable = new LpSummand[0];
        public IEnumerable<LpSummand> Summands { get; }
        public int SummandsCount { get; }
#else
        private static IReadOnlyList<LpSummand> EmptyList = new ReadOnlyCollection<LpSummand>(new List<LpSummand>());

        public IReadOnlyList<LpSummand> Summands { get; }
#endif

        public LpSum(IEnumerable<LpSummand> summands)
        {
#if NET20
            var list = new List<LpSummand>(summands);
            this.Summands = list;
            this.SummandsCount = list.Count;
#else
            this.Summands = summands.ToList();
#endif
        }

#if NET20
        private LpSum(IEnumerable<LpSummand> summands, int summandsCount)
        {
            this.Summands = summands;
            this.SummandsCount = summandsCount;
        }
        public static LpSum Empty() => new LpSum(EmptyEnumerable, 0);
#else
        private LpSum(IReadOnlyList<LpSummand> summands)
        {
            this.Summands = summands;
        }
        public static LpSum Empty() => new LpSum(EmptyList);
#endif

        internal LpSum(params LpSummand[] summands)
        {
            this.Summands = summands;
#if NET20
            this.SummandsCount = summands.Length;
#endif
        }

        public LpSum Add(LpSummand summand)
        {
#if NET20 || NETSTANDARD1_5
            var copy = new List<LpSummand>(this.Summands);
            copy.Add(summand);
            return new LpSum(copy);
#else
            return new LpSum(this.Summands.Append(summand));
#endif
        }

        public LpSum Add(LpSum sum)
        {
#if NET20 || NETSTANDARD1_5
            var copy = new List<LpSummand>(this.Summands);
            copy.AddRange(sum.Summands);
            return new LpSum(copy);
#else
            return new LpSum(this.Summands.Concat(sum.Summands));
#endif
        }

        public LpSum Subtract(LpVariable variable)
        {
#if NET20 || NETSTANDARD1_5
            var copy = new List<LpSummand>(this.Summands);
            copy.Add(-variable);
            return new LpSum(copy);
#else
            return new LpSum(this.Summands.Append(-variable));
#endif
        }

        public LpSum Subtract(LpSummand summand)
        {
#if NET20 || NETSTANDARD1_5
            var copy = new List<LpSummand>(this.Summands);
            copy.Add(-summand);
            return new LpSum(copy);
#else
            return new LpSum(this.Summands.Append(-summand));
#endif
        }

        public LpSum Subtract(LpSum sum)
        {
#if NET20 || NETSTANDARD1_5
            var copy = new List<LpSummand>(this.Summands);
            foreach (var summand in sum.Summands)
            {
                copy.Add(-summand);
            }
            return new LpSum(copy);
#else
            return new LpSum(
                this.Summands.Concat(sum.Summands.Select(s => -s)));
#endif
        }

        public static LpSum operator +(LpSum left, LpVariable right)
            => left.Add(right);

        public static LpSum operator +(LpSum left, LpSummand right)
            => left.Add(right);

        public static LpSum operator +(LpSum left, LpSum right)
            => left.Add(right);

        public static LpSum operator -(LpSum sum)
#if NET20
            => LpSum.Empty().Subtract(sum);
#else
            => new LpSum(sum.Summands.Select(s => -s));
#endif

        public static LpSum operator -(LpSum left, LpVariable right)
            => left.Subtract(right);

        public static LpSum operator -(LpSum left, LpSummand right)
            => left.Subtract(right);

        public static LpSum operator -(LpSum left, LpSum right)
            => left.Subtract(right);

        public static LpSum operator *(LpSum sum, double factor)
#if NET20
        {
            var newSummands = new List<LpSummand>();
            foreach (var summand in sum.Summands)
            {
                newSummands.Add(summand * factor);
            }

            return new LpSum(newSummands, newSummands.Count);
        }
#else
            => new LpSum(sum.Summands.Select(s => s * factor));
#endif
        public static LpSum operator *(double factor, LpSum sum)
#if NET20
            => sum * factor;
#else
            => new LpSum(sum.Summands.Select(s => factor * s));
#endif

        public static LpSumConstraint operator <=(LpSum sum, double value)
            => new LpSumConstraint(sum, lpsolve_constr_types.LE, value);

        public static LpSumConstraint operator >=(LpSum sum, double value)
            => new LpSumConstraint(sum, lpsolve_constr_types.GE, value);

        public static LpSumConstraint operator ==(LpSum sum, double value)
            => new LpSumConstraint(sum, lpsolve_constr_types.EQ, value);

        [Obsolete(Errors.InequalityNotSupported)]
        public static LpSumConstraint operator !=(LpSum sum, double value)
            => throw new InvalidOperationException(Errors.InequalityNotSupported);
    }

    public readonly struct LpSummandConstraint
    {
        public LpSummandConstraint(
            LpSummand summand,
            lpsolve_constr_types constraintType,
            double rightHandSide)
        {
            Summand = summand;
            ConstraintType = constraintType;
            RightHandSide = rightHandSide;
        }
        public LpSummand Summand { get; }

        public lpsolve_constr_types ConstraintType { get; }

        public double RightHandSide { get; }
    }

    public readonly struct LpSumConstraint
    {
        public LpSumConstraint(
            LpSum sum,
            lpsolve_constr_types constraintType,
            double rightHandSide)
        {
            Sum = sum;
            ConstraintType = constraintType;
            RightHandSide = rightHandSide;
        }

        public LpSum Sum { get; }
        public lpsolve_constr_types ConstraintType { get; }
        public double RightHandSide { get; }
    }
}