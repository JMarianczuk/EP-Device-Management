//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Threading;
//using LpSolveDotNet;

//namespace LpSolverBuilder
//{
//    public class LpVariable : LpTerm
//    {
//        public LpVariable(int columnNumber)
//        {
//            ColumnNumber = columnNumber;
//        }

//        public int ColumnNumber { get; }

//#if DEBUG
//        public string Name { get; set; }

//        public override string ToString()
//        {
//            return Name;
//        }
//#endif
//    }

//    public class LpSummand : LpTerm
//    {
//        public LpSummand(LpVariable variable, double factor)
//        {
//            Variable = variable;
//            Factor = factor;
//        }
//        public LpVariable Variable { get; }

//        public double Factor { get; }

//        public override string ToString()
//        {
//            return $"{Factor:00.000} * {Variable}";
//        }
//    }

//    public class LpSum : LpTerm
//    {
//        private static IReadOnlyList<LpSummand> EmptyList =
//            new ReadOnlyCollection<LpSummand>(new List<LpSummand>());

//        public IReadOnlyList<LpSummand> Summands { get; }

//        public LpSum()
//        {
//            this.Summands = EmptyList;
//        }

//        public LpSum(IEnumerable<LpSummand> summands)
//        {
//            this.Summands = summands.ToList();
//        }

//        public LpSum Add(LpTerm term)
//        {
//            switch (term)
//            {
//                case LpVariable v:
//                    return new LpSum(this.Summands.Append(new LpSummand(v, 1)));
//                case LpSummand s:
//                    return new LpSum(this.Summands.Append(s));
//                case LpSum other:
//                    return new LpSum(this.Summands.Concat(other.Summands));
//                default:
//                    throw new ArgumentException($"unknown term of type '{term.GetType()}'");
//            }
//        }

//        public LpSum Subtract(LpTerm term)
//        {
//            switch (term)
//            {
//                case LpVariable v:
//                    return new LpSum(this.Summands.Append(new LpSummand(v, -1)));
//                case LpSummand s:
//                    return new LpSum(this.Summands.Append(new LpSummand(s.Variable, -s.Factor)));
//                case LpSum other:
//                    return new LpSum(
//                        this.Summands.Concat(other.Summands.Select(s => new LpSummand(s.Variable, -s.Factor))));
//                default:
//                    throw new ArgumentException($"unknown term of type '{term.GetType()}'");
//            }
//        }

//        public override string ToString()
//        {
//            return string.Join(", ", this.Summands);
//        }
//    }

//    public abstract class LpTerm
//    {
//        public static LpSum operator +(LpTerm left, LpTerm right)
//        {
//            if (left is LpSum leftSum)
//            {
//                return leftSum.Add(right);
//            }
//            else if (right is LpSum rightSum)
//            {
//                return rightSum.Add(left);
//            }
//            else
//            {
//                return new LpSum().Add(left).Add(right);
//            }
//        }

//        public static LpSum operator -(LpTerm left, LpTerm right)
//        {
//            if (left is LpSum leftSum)
//            {
//                return leftSum.Subtract(right);
//            }
//            else
//            {
//                return new LpSum().Add(left).Subtract(right);
//            }
//        }

//        public static LpTerm operator *(LpTerm term, double factor)
//        {
//            switch (term)
//            {
//                case LpVariable v:
//                    return new LpSummand(v, factor);
//                case LpSummand s:
//                    return new LpSummand(s.Variable, s.Factor * factor);
//                case LpSum sum:
//                    var result = new LpSum(sum.Summands.Select(s => new LpSummand(s.Variable, s.Factor * factor)));
//                    return result;
//                default:
//                    throw new NotImplementedException();
//            }
//        }

//        public static LpTerm operator *(double factor, LpTerm term)
//        {
//            return term * factor;
//        }

//        public static LpTerm operator -(LpTerm term)
//        {
//            return new LpSum().Subtract(term);
//        }

//        public static LpConstraint operator <=(LpTerm left, double right)
//        {
//            return new LpConstraint(left, lpsolve_constr_types.LE, right);
//        }

//        public static LpConstraint operator >=(LpTerm left, double right)
//        {
//            return new LpConstraint(left, lpsolve_constr_types.GE, right);
//        }

//        public static LpConstraint operator ==(LpTerm left, double right)
//        {
//            return new LpConstraint(left, lpsolve_constr_types.EQ, right);
//        }

//        [Obsolete("Inequality is not supported by " + nameof(lpsolve_constr_types))]
//        public static LpConstraint operator !=(LpTerm left, double right)
//        {
//            throw new InvalidOperationException("Inequality is not supported by " + nameof(lpsolve_constr_types));
//        }
//    }

//    public readonly struct LpConstraint
//    {
//        public LpConstraint(
//            LpTerm term,
//            lpsolve_constr_types constraintType,
//            double rightHandSide)
//        {
//            Term = term;
//            ConstraintType = constraintType;
//            RightHandSide = rightHandSide;
//        }

//        public LpTerm Term { get; }

//        public lpsolve_constr_types ConstraintType { get; }

//        public double RightHandSide { get; }

//        public void Deconstruct(out LpTerm term, out lpsolve_constr_types constraintType, out double rightHandSide)
//        {
//            term = this.Term;
//            constraintType = this.ConstraintType;
//            rightHandSide = this.RightHandSide;
//        }
//    }
//}