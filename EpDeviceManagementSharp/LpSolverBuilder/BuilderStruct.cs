//using System;
//using System.Collections.Generic;
//using System.Linq;
//using LpSolveDotNet;

//namespace LpSolverBuilder
//{
//    public class BuilderStruct
//    {
//        private readonly IList<LpConstraintStruct> constraints;

//        public BuilderStruct()
//        {
//            this.constraints = new List<LpConstraintStruct>();
//        }

//        public BuilderStruct AddConstraint(LpSumStruct sum, lpsolve_constr_types equality, double result)
//        {
//            this.constraints.Add(new LpConstraintStruct(sum, equality, result));
//            return this;
//        }

//        public BuilderStruct AddConstraint(LpConstraintStruct constraint)
//        {
//            this.constraints.Add(constraint);
//            return this;
//        }

//        public LpSolve CreateSolver(LpSumStruct objectiveFunction, bool maximize)
//        {
//            var sums = constraints.Select(c => c.Sum).ToList();

//            var maxVariablesPerConstraint = 0;
//            var maxColumnNumber = 0;
//            foreach (var sum in sums.Append(objectiveFunction))
//            {
//                maxVariablesPerConstraint = Math.Max(maxVariablesPerConstraint, sum.Summands.Count);
//                maxColumnNumber = Math.Max(maxColumnNumber, sum.Summands.Max(s => s.Variable.ColumnNumber));
//            }
//            var solver = LpSolve.make_lp(0, maxColumnNumber);

//            var constraintColumns = new int[maxVariablesPerConstraint];
//            var constraintRow = new double[maxVariablesPerConstraint];

//            int ExtractFactors(LpSumStruct sum)
//            {
//                for (int i = 0; i < sum.Summands.Count; i += 1)
//                {
//                    constraintColumns[i] = sum.Summands[i].Variable.ColumnNumber;
//                    constraintRow[i] = sum.Summands[i].Factor;
//                }
//                return sum.Summands.Count;
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