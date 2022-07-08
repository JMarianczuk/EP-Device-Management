//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using LpSolveDotNet;

//namespace LpSolverBuilder
//{
//    public class Builder
//    {
//        private readonly IList<LpConstraint> constraints;

//        public Builder()
//        {
//            this.constraints = new List<LpConstraint>();
//        }

//        public Builder AddConstraint(LpTerm term, lpsolve_constr_types equality, double result)
//        {
//            this.constraints.Add(new LpConstraint(term, equality, result));
//            return this;
//        }

//        public Builder AddConstraint(LpConstraint constraint)
//        {
//            this.constraints.Add(constraint);
//            return this;
//        }

//        public LpSolve CreateSolver(LpTerm objectiveFunction, bool maximize)
//        {
//            var terms = constraints.Select(c => c.Term).ToList();

//            var maxVariablesPerConstraint = 0;
//            var maxColumnNumber = 0;
//            foreach (var term in terms.Append(objectiveFunction))
//            {
//                switch (term)
//                {
//                    case LpVariable v:
//                        maxVariablesPerConstraint = Math.Max(maxVariablesPerConstraint, 1);
//                        maxColumnNumber = Math.Max(maxColumnNumber, v.ColumnNumber);
//                        break;
//                    case LpSummand s:
//                        maxVariablesPerConstraint = Math.Max(maxVariablesPerConstraint, 1);
//                        maxColumnNumber = Math.Max(maxColumnNumber, s.Variable.ColumnNumber);
//                        break;
//                    case LpSum sum:
//                        maxVariablesPerConstraint = Math.Max(maxVariablesPerConstraint, sum.Summands.Count);
//                        maxColumnNumber = Math.Max(maxColumnNumber, sum.Summands.Max(s => s.Variable.ColumnNumber));
//                        break;
//                }
//            }
//            var solver = LpSolve.make_lp(0, maxColumnNumber);

//            var constraintColumns = new int[maxVariablesPerConstraint];
//            var constraintRow = new double[maxVariablesPerConstraint];

//            int ExtractFactors(LpTerm lpTerm)
//            {
//                int length = 1;
//                switch (lpTerm)
//                {
//                    case LpVariable v:
//                        constraintColumns[0] = v.ColumnNumber;
//                        constraintRow[0] = 1;
//                        break;
//                    case LpSummand s:
//                        constraintColumns[0] = s.Variable.ColumnNumber;
//                        constraintRow[0] = s.Factor;
//                        break;
//                    case LpSum sum:
//                        for (int i = 0; i < sum.Summands.Count; i += 1)
//                        {
//                            constraintColumns[i] = sum.Summands[i].Variable.ColumnNumber;
//                            constraintRow[i] = sum.Summands[i].Factor;
//                        }

//                        length = sum.Summands.Count;
//                        break;
//                }

//                return length;
//            }

//            var objectiveFunctionCount = ExtractFactors(objectiveFunction);
//            solver.set_obj_fnex(objectiveFunctionCount, constraintRow, constraintColumns);
//            if (maximize)
//            {
//                solver.set_maxim();
//            }
//            else
//            {
//                solver.set_minim();
//            }
//            solver.resize_lp(this.constraints.Count, maxColumnNumber);
//            solver.set_add_rowmode(true);

//            foreach (var (term, type, value) in this.constraints)
//            {
//                var length = ExtractFactors(term);

//                solver.add_constraintex(length, constraintRow, constraintColumns, type, value);
//            }
//            solver.set_add_rowmode(false);
//            return solver;
//        }
//    }
//}
