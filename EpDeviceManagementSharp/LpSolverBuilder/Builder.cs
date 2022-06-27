using System;
using System.Collections.Generic;
using System.Linq;
using LpSolveDotNet;

namespace LpSolverBuilder
{
    public class Builder
    {
        private readonly IList<(LpTerm, LpConstraintType, double)> constraints;

        public Builder()
        {
            this.constraints = new List<(LpTerm, LpConstraintType, double)>();
        }

        public Builder AddConstraint(LpTerm term, LpConstraintType equality, double result)
        {
            this.constraints.Add((term, equality, result));
            return this;
        }

        public LpSolve CreateSolver()
        {
            var terms = constraints.Select(triple => triple.Item1).ToList();
            IDictionary<int, LpVariable> allVariables = new Dictionary<int, LpVariable>();

            void Add(LpTerm term)
            {
                switch (term)
                {
                    case LpVariable var:
                        allVariables[var.Id] = var;
                        break;
                    case LpSum sum:
                        Add(sum.Left);
                        Add(sum.Right);
                        break;
                    case LpDifference diff:
                        Add(diff.Left);
                        Add(diff.Right);
                        break;
                }
            }
            foreach (var term in terms)
            {
                Add(term);
            }

            var variables = allVariables.Values.ToList();
            var solver = LpSolve.make_lp(0, variables.Count);

        }
    }
}
