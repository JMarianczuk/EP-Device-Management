using System.Net.NetworkInformation;
using UnitsNet;

namespace MpcForBuildings
{
    public class MpcFormulation
    {
        public delegate Temperature[] f(Temperature[] x_k, Power[] u_k, IQuantity[] d_k);

        public delegate Temperature[] g(Temperature[] x_k, Power[] u_k, IQuantity[] d_k);

        public delegate Power[] f_HVAC(Temperature[] x_k, Ratio[] a_k, IQuantity[] m_k);

        public delegate Temperature[] h(
            Temperature[] x_k,
            Temperature[] y_k,
            Power[] u_k,
            Temperature[] r_k);

        public delegate IQuantity[] d(double t, int k);

        public delegate Temperature[] r(double t, int k);

        public delegate double l_N(Temperature[] x_N);

        public delegate double l_k(
            Temperature[] x_k,
            Temperature[] y_k,
            Temperature[] r_k,
            Power[] u_k,
            Temperature[] s_k);

        private T[][] Create<T>(int dimension1, int dimension2)
        {
            var result = new T[dimension1][];
            for (int i = 0; i < dimension1; i += 1)
            {
                result[i] = new T[dimension2];
            }

            return result;
        }
        private void Copy<T>(T[,] source, int column, T[] destination)
        {
            for (int i = 0; i < source.GetLength(1); i += 1)
            {
                destination[i] = source[column, i];
            }
        }

        public Ratio[][] Solve(
            int N,
            Temperature[] x_0,
            f f,
            g g,
            f_HVAC f_HVAC,
            h h,
            d d,
            r r,
            l_N l_N,
            l_k l_k,
            int nx,
            int ny,
            int nu,
            int na,
            int nm,
            int nd,
            int nr,
            int ns)
        {
            var x = Create<Temperature>(N+1, nx);
            x[0] = x_0;
            var y = Create<Temperature>(N, ny);
            var u = Create<Power>(N, nu);
            var s = Create<Temperature>(N, ns);
            var d_ = Create<IQuantity>(N, nd);
            var r_ = Create<Temperature>(N, nd);
            var a = Create<Ratio>(N, na);
            var m = Create<IQuantity>(N, nm);
            
            double Eval()
            {
                var result = l_N(x[N]);
                for (int k = 0; k < N; k += 1)
                {
                    result += l_k(x[k], y[k], r_[k], u[k], s[k]);
                }
                return result;
            }
            
            void Calculate()
            {
                for (int k = 0; k < N; k += 1)
                {
                    u[k] = f_HVAC(x[k], a[k], m[k]);
                    d_[k] = d(0, k);
                    r_[k] = r(0, k);
                    y[k] = g(x[k], u[k], d_[k]);
                    s[k] = h(x[k], y[k], u[k], r_[k]);
                    x[k + 1] = f(x[k], u[k], d_[k]);
                }
            }
            
            var currentScore = double.MaxValue;
            var current_a = Create<Ratio>(N, na);
            void Permute(int k, int a_i)
            {
                for (int i = 0; i <= 10; i += 1)
                {
                    a[k][a_i] = Ratio.FromPercent(10 * i);
                    if (a_i + 1 < a[k].Length)
                    {
                        Permute(k, a_i+1);
                    }
                    else if (k + 1 < a.Length)
                    {
                        Permute(k + 1, 0);
                    }
                    else
                    {
                        Calculate();
                        var result = Eval();
                        if (result < currentScore)
                        {
                            currentScore = result;
                            Array.Copy(a, current_a, a.Length);
                        }
                    }
                }
            }
            Permute(0, 0);
            return current_a;
        }
    }
}