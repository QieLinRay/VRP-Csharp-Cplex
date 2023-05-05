using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILOG.CPLEX;
using ILOG.Concert;

namespace VRP_2023_5
{
    internal class Program
    {

        public void VRP()
        {
            Cplex model = new Cplex();
            Random rnd = new Random(1);
            #region
            //sets and parameters
            int N = 10;
            int K = 3;
            int M = 9999;
            int[] q_n = new int[N + 2];
            int[] x_i = new int[N + 2];
            int[] y_i = new int[N + 2];
            int Q = 100;
            int x_max = 50;
            int y_max = 50;
            double[][] d_nn = new double[N + 2][];

            for (int n = 0; n < N + 2; n++)
            {
                q_n[n] = rnd.Next(30);
                x_i[n] = rnd.Next(x_max);
                y_i[n] = rnd.Next(y_max);

            }

            for (int n = 0; n < N + 2; n++)
            {
                d_nn[n] = new double[N + 2];
                if (n > 1 && n < N + 1)
                {
                    for (int m = 0; m < N + 2; m++)
                    {
                        double x = (x_i[n] - x_i[m]) * (x_i[n] - x_i[m]);
                        double y = (y_i[n] - y_i[m]) * (y_i[n] - y_i[m]);
                        d_nn[n][m] = Math.Abs(Math.Sqrt(x * y));


                    }


                }
            }
            #endregion

            #region
            //decision variables

            INumVar[][] alpha = new INumVar[N + 2][];
            INumVar[][] gamma = new INumVar[N + 2][];
            INumVar[][][] beta = new INumVar[K][][];

            for (int n = 0; n < N + 2; n++)
            {
                alpha[n] = new INumVar[N + 2];
                for (int k = 0; k < K; k++)
                {
                    alpha[n][k] = model.NumVar(0, 1, NumVarType.Bool);
                }
            }

            for (int n = 0; n < N + 2; n++)
            {
                gamma[n] = new INumVar[N + 2];
                for (int k = 0; k < K; k++)
                {
                    gamma[n][k] = model.NumVar(0, 9999, NumVarType.Float);
                }
            }

            for (int k = 0; k < K; k++)
            {
                beta[k] = new INumVar[N + 2][];
                for (int n = 0; n < N + 2; n++)
                {
                    beta[k][n] = new INumVar[N + 2];
                    for (int n1 = 0; n1 < N + 2; n1++)
                    {
                        beta[k][n][n1] = model.NumVar(0, n == n1 ? 0 : 1, NumVarType.Bool);
                    }

                }
            }

            #endregion

            #region
            //obj
            INumExpr[] obj = new INumExpr[K];
            obj[0] = model.NumExpr();
            for (int k = 0; k < K; k++)
            {
                for (int n = 0; n < N + 1; n++)
                {
                    for (int n1 = 1; n1 < N + 2; n1++)
                    {
                        obj[0] = model.Sum(obj[0], model.Prod(d_nn[n][n1], beta[k][n][n1]));

                    }
                }
            }
            model.AddMinimize(obj[0]);




            #endregion

            #region
            //subjective

            //node 1
            INumExpr[] expr1 = new INumExpr[K];
            for (int n = 1; n < N + 1; n++)
            {
                expr1[0] = model.NumExpr();
                for (int k = 0; k < K; k++)
                {
                    expr1[0] = model.Sum(expr1[0], alpha[n][k]);
                }
                model.AddEq(expr1[0], 1);

            }

            //Quantity  2
            INumExpr[] expr2 = new INumExpr[K];
            for (int k = 0; k < K; k++)
            {
                expr2[0] = model.NumExpr();
                for (int n = 1; n < N + 1; n++)
                {
                    expr2[0] = model.Sum(expr2[0], model.Prod(q_n[n], alpha[n][k]));
                }
                model.AddLe(expr2[0], Q);
            }

            //Flow    3

            INumExpr[] expr31 = new INumExpr[K];
            INumExpr[] expr32 = new INumExpr[K];

            for (int k = 0; k < K; k++)
            {
                for (int n = 1; n < N + 1; n++)

                {
                    expr31[0] = model.NumExpr();
                    expr32[0] = model.NumExpr();

                    for (int n1 = 1; n1 < N + 2; n1++)
                    {
                        expr31[0] = model.Sum(expr31[0], beta[k][n][n1]);
                    }
                    for (int n1 = 0; n1 < N + 1; n1++)
                    {
                        expr32[0] = model.Sum(expr32[0], beta[k][n1][n]);
                    }

                    model.AddEq(expr31[0], expr32[0]);
                    model.AddEq(expr31[0], alpha[n][k]);

                }
            }

            //  dummy first last
            INumExpr[] expr41 = new INumExpr[K];
            INumExpr[] expr42 = new INumExpr[K];

            for (int k = 0; k < K; k++)
            {
                expr41[0] = model.NumExpr();
                expr42[0] = model.NumExpr();
                for (int n = 0; n < N + 1; n++)
                {
                    expr41[0] = model.Sum(expr41[0], beta[k][n][N + 1]);

                }

                for (int n = 1; n < N + 2; n++)
                {
                    expr42[0] = model.Sum(expr42[0], beta[k][0][n]);
                }
                model.AddEq(expr41[0], expr42[0]);
                model.AddEq(expr41[0], 1);
            }

            //sub
            INumExpr[] expr5 = new INumExpr[K];
            for (int k = 0; k < K; k++)
            {
                for (int n = 0; n < N + 1; n++)
                {
                    for (int n1 = 1; n1 < N + 2; n1++)
                    {
                        expr5[0] = model.NumExpr();
                        model.AddGe(gamma[n1][k], model.Diff(model.Sum(gamma[n][k], d_nn[n][n1]), model.Prod(M, model.Diff(1, beta[k][n][n1]))));
                    }
                }
            }

            #endregion

            if (model.Solve())
            {
                Console.WriteLine(model.ObjValue);
                for (int k = 0; k < K; k++)
                {
                    for (int n = 0; n < N + 1; n++)
                    {
                        for (int n1 = 1; n1 < N + 2; n1++)
                        {
                            if (model.GetValue(beta[k][n][n1]) > 0.9)
                            {
                                try
                                {
                                    Console.WriteLine("beta[{0}][{1}][{2}]={3} {4}", k, n, n1, model.GetValue(beta[k][n][n1]), model.GetValue(gamma[n][k]));
                                }
                                catch
                                {

                                }

                            }

                        }
                    }
                    Console.WriteLine();
                }

                for (int k = 0; k < K; k++)
                {
                    for (int n = 1; n < N + 1; n++)
                    {
                        if (model.GetValue(alpha[n][k]) > 0.9)
                        {

                            try
                            {
                                Console.WriteLine("alpha[{0}][{1}]={2}", n, k, model.GetValue(alpha[n][k]));
                            }
                            catch
                            {

                            }

                        }

                    }
                }
            }
            else
            {
                Console.WriteLine("no solution");
            }


        }
        static void Main(string[] args)
        {
            Program a = new Program();
            a.VRP();
        }
    }
}
