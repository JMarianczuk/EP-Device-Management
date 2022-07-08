//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Data.Common;
//using System.Linq;
//using System.Threading.Tasks;
//using LpSolveDotNet;

//namespace LpSolverBuilder
//{
//    public readonly struct LpVariableStruct
//    {
//        public LpVariableStruct(int columnNumber)
//        {
//            ColumnNumber = columnNumber;
//        }

//        public int ColumnNumber { get; }

//        public static implicit operator LpSummandStruct(LpVariableStruct variable)
//            => new LpSummandStruct(variable, 1);

//        public static LpSumStruct operator +(LpVariableStruct left, LpVariableStruct right)
//        {
//            return LpSumStruct.From(left, right);
//        }

//        public static LpSumStruct operator +(LpVariableStruct left, LpSummandStruct right)
//        {
//            return LpSumStruct.From(left, right);
//        }

//        public static LpSumStruct operator +(LpVariableStruct left, LpSumStruct right)
//        {
//            return right.Add(left);
//        }

//        public static LpSummandStruct operator -(LpVariableStruct variable)
//        {
//            return new LpSummandStruct(variable, -1);
//        }

//        public static LpSumStruct operator -(LpVariableStruct left, LpVariableStruct right)
//        {
//            return LpSumStruct.From(left, -right);
//        }

//        public static LpSumStruct operator -(LpVariableStruct left, LpSummandStruct right)
//        {
//            return LpSumStruct.From(left, -right);
//        }

//        public static LpSumStruct operator -(LpVariableStruct left, LpSumStruct right)
//        {
//            return LpSumStruct.From(left).Subtract(right);
//        }
        
//        public static LpSummandStruct operator *(LpVariableStruct variable, double factor)
//        {
//            return new LpSummandStruct(variable, factor);
//        }
        
//        public static LpSummandStruct operator *(double factor, LpVariableStruct variable)
//        {
//            return new LpSummandStruct(variable, factor);
//        }

//        public static LpConstraintStruct operator <=(LpVariableStruct variable, double rightHandSide)
//        {
//            return new LpConstraintStruct(LpSumStruct.From(variable), lpsolve_constr_types.LE, rightHandSide);
//        }

//        public static LpConstraintStruct operator >=(LpVariableStruct variable, double rightHandSide)
//        {
//            return new LpConstraintStruct(LpSumStruct.From(variable), lpsolve_constr_types.GE, rightHandSide);
//        }

//        public static LpConstraintStruct operator ==(LpVariableStruct variable, double rightHandSide)
//        {
//            return new LpConstraintStruct(LpSumStruct.From(variable), lpsolve_constr_types.EQ, rightHandSide);
//        }

//        [Obsolete("Inequality is not supported by " + nameof(lpsolve_constr_types))]
//        public static LpConstraintStruct operator !=(LpVariableStruct variable, double rightHandSide)
//        {
//            throw new InvalidOperationException("Inequality is not supported by " + nameof(lpsolve_constr_types));
//        }
//    }

//    public readonly struct LpSummandStruct
//    {
//        public LpSummandStruct(LpVariableStruct variable, double factor)
//        {
//            Variable = variable;
//            Factor = factor;
//        }

//        public LpVariableStruct Variable { get; }

//        public double Factor { get; }

//        public static LpSumStruct operator +(LpSummandStruct left, LpVariableStruct right)
//        {
//            return LpSumStruct.From(left, right);
//        }

//        public static LpSumStruct operator +(LpSummandStruct left, LpSummandStruct right)
//        {
//            return LpSumStruct.From(left, right);
//        }

//        public static LpSumStruct operator +(LpSummandStruct left, LpSumStruct right)
//        {
//            return right.Add(left);
//        }

//        public static LpSummandStruct operator -(LpSummandStruct summand)
//        {
//            return new LpSummandStruct(summand.Variable, -summand.Factor);
//        }

//        public static LpSumStruct operator -(LpSummandStruct left, LpVariableStruct right)
//        {
//            return LpSumStruct.From(left, -right);
//        }

//        public static LpSumStruct operator -(LpSummandStruct left, LpSummandStruct right)
//        {
//            return LpSumStruct.From(left, -right);
//        }

//        public static LpSumStruct operator -(LpSummandStruct left, LpSumStruct right)
//        {
//            return LpSumStruct.From(left).Subtract(right);
//        }

//        public static LpSummandStruct operator *(LpSummandStruct summand, double factor)
//        {
//            return new LpSummandStruct(summand.Variable, summand.Factor * factor);
//        }

//        public static LpSummandStruct operator *(double factor, LpSummandStruct summand)
//        {
//            return new LpSummandStruct(summand.Variable, summand.Factor * factor);
//        }

//        public static LpConstraintStruct operator <=(LpSummandStruct summand, double rightHandSide)
//        {
//            return new LpConstraintStruct(LpSumStruct.From(summand), lpsolve_constr_types.LE, rightHandSide);
//        }

//        public static LpConstraintStruct operator >=(LpSummandStruct summand, double rightHandSide)
//        {
//            return new LpConstraintStruct(LpSumStruct.From(summand), lpsolve_constr_types.GE, rightHandSide);
//        }

//        public static LpConstraintStruct operator ==(LpSummandStruct summand, double rightHandSide)
//        {
//            return new LpConstraintStruct(LpSumStruct.From(summand), lpsolve_constr_types.EQ, rightHandSide);
//        }

//        [Obsolete("Inequality is not supported by " + nameof(lpsolve_constr_types))]
//        public static LpConstraintStruct operator !=(LpSummandStruct summand, double rightHandSide)
//        {
//            throw new InvalidOperationException("Inequality is not supported by " + nameof(lpsolve_constr_types));
//        }
//    }

//    public readonly struct LpSumStruct
//    {
//        private static IReadOnlyList<LpSummandStruct> EmptyList =
//            new ReadOnlyCollection<LpSummandStruct>(new List<LpSummandStruct>());

//        public LpSumStruct(IEnumerable<LpSummandStruct> summands)
//        {
//            this.Summands = summands.ToList();
//        }

//        private LpSumStruct(LpSummandStruct[] summands)
//        {
//            this.Summands = summands;
//        }

//        internal static LpSumStruct From(params LpSummandStruct[] summands)
//        {
//            return new LpSumStruct(summands);
//        }

//        public IReadOnlyList<LpSummandStruct> Summands { get; }

//        public LpSumStruct Add(LpSummandStruct summand)
//        {
//            return new LpSumStruct(this.Summands.Append(summand));
//        }

//        public LpSumStruct Add(LpSumStruct other)
//        {
//            return new LpSumStruct(this.Summands.Concat(other.Summands));
//        }

//        public LpSumStruct Subtract(LpVariableStruct variable)
//        {
//            return new LpSumStruct(this.Summands.Append(-variable));
//        }

//        public LpSumStruct Subtract(LpSummandStruct summand)
//        {
//            return new LpSumStruct(this.Summands.Append(-summand));
//        }

//        public LpSumStruct Subtract(LpSumStruct other)
//        {
//            return new LpSumStruct(
//                this.Summands.Concat(other.Summands.Select(s => -s)));
//        }

//        public static LpSumStruct operator +(LpSumStruct left, LpVariableStruct right)
//        {
//            return left.Add(right);
//        }

//        public static LpSumStruct operator +(LpSumStruct left, LpSummandStruct right)
//        {
//            return left.Add(right);
//        }

//        public static LpSumStruct operator +(LpSumStruct left, LpSumStruct right)
//        {
//            return left.Add(right);
//        }

//        public static LpSumStruct operator -(LpSumStruct sum)
//        {
//            return new LpSumStruct(sum.Summands.Select(s => -s));
//        }

//        public static LpSumStruct operator -(LpSumStruct left, LpVariableStruct right)
//        {
//            return left.Subtract(right);
//        }

//        public static LpSumStruct operator -(LpSumStruct left, LpSummandStruct right)
//        {
//            return left.Subtract(right);
//        }

//        public static LpSumStruct operator -(LpSumStruct left, LpSumStruct right)
//        {
//            return left.Subtract(right);
//        }

//        public static LpSumStruct operator *(LpSumStruct sum, double factor)
//        {
//            return new LpSumStruct(sum.Summands.Select(s => s * factor));
//        }

//        public static LpSumStruct operator *(double factor, LpSumStruct sum)
//        {
//            return new LpSumStruct(sum.Summands.Select(s => s * factor));
//        }

//        public static LpConstraintStruct operator <=(LpSumStruct sum, double rightHandSide)
//        {
//            return new LpConstraintStruct(sum, lpsolve_constr_types.LE, rightHandSide);
//        }

//        public static LpConstraintStruct operator >=(LpSumStruct sum, double rightHandSide)
//        {
//            return new LpConstraintStruct(sum, lpsolve_constr_types.GE, rightHandSide);
//        }

//        public static LpConstraintStruct operator ==(LpSumStruct sum, double rightHandSide)
//        {
//            return new LpConstraintStruct(sum, lpsolve_constr_types.EQ, rightHandSide);
//        }

//        [Obsolete("Inequality is not supported by " + nameof(lpsolve_constr_types))]
//        public static LpConstraintStruct operator !=(LpSumStruct sum, double rightHandSide)
//        {
//            throw new InvalidOperationException("Inequality is not supported by " + nameof(lpsolve_constr_types));
//        }
//    }

//    public readonly struct LpConstraintStruct
//    {
//        public LpConstraintStruct(
//            LpSumStruct sum,
//            lpsolve_constr_types constraintType,
//            double rightHandSide)
//        {
//            Sum = sum;
//            ConstraintType = constraintType;
//            RightHandSide = rightHandSide;
//        }
        
//        public LpSumStruct Sum { get; }

//        public lpsolve_constr_types ConstraintType { get; }

//        public double RightHandSide { get; }

//        public void Deconstruct(out LpSumStruct sum, out lpsolve_constr_types constraintType, out double rightHandSide)
//        {
//            sum = this.Sum;
//            constraintType = this.ConstraintType;
//            rightHandSide = this.RightHandSide;
//        }
//    }
//}