using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace CI_2
{
    class Program
    {
        static int N = 3, Nsq, MaxRestarts = 30;
        static int[] Sudoku;
        static Random rng;
        static List<int> startArray;
        static string SudokuPath = "E:\\Documents\\Visual Studio 2015\\Projects\\CI 2\\CI 2\\TestSudokuVeryEz.txt";
        static List<int>[] OpenIdxPerBlock, ClosedNumsPerBlock, OpenNumsPerBlock;

        static bool RRHC_OptFound;
        static int RRHC_Restarts, RRHC_States_Global, RRHC_States_Local, RRHC_States_Avg;
        static int[] RRHC_Best_So_Far;

        static void Main( string[] args )
        {
            Nsq = N * N;
            rng = new Random();

            ReadSudoku();

            OpenIdxPerBlock = new List<int>[ Nsq ];
            ClosedNumsPerBlock = new List<int>[ Nsq ];
            OpenNumsPerBlock = new List<int>[ Nsq ];

            // For each block, find out which numbers are missing, and which fields are empty.
            for ( int i = 0; i < Nsq; i++ )
            {
                OpenIdxPerBlock[ i ] = new List<int>();
                ClosedNumsPerBlock[ i ] = new List<int>();
                OpenNumsPerBlock[ i ] = new List<int>();
                int[] blockIdx = GetBlockIndices( i );

                // Check each field.
                for ( int j = 0; j < Nsq; j++ )
                {
                    if ( Sudoku[ blockIdx[ j ] ] == 0 )
                        OpenIdxPerBlock[ i ].Add( blockIdx[ j ] );
                    else
                        ClosedNumsPerBlock[ i ].Add( Sudoku[ blockIdx[ j ] ] );
                }

                // The missing numbers is the complement set of the numbers we already have.
                for ( int j = 1; j <= Nsq; j++ )
                    if ( !ClosedNumsPerBlock[ i ].Contains( j ) )
                        OpenNumsPerBlock[ i ].Add( j );
            }

            FillSudoku();
            // After this, the sudoku array has been initiated, every block contains numbers one through nine, having kept in mind the constraint of blocks, and without having moved the fixated spots.

            RandomRestart();
            PrintRRHCResults();
            //Print();
            //Operator();
            //Print();
            //Operator();
            //Print();
        }

        static void RandomRestart()
        {
            bool OperatorResult;

            for ( int i = 0; i < MaxRestarts; i++ )
            {
                OperatorResult = true;

                // Keep applying the operator until we have found an optimum
                while ( OperatorResult )
                {
                    RRHC_States_Global++;
                    RRHC_States_Local++;
                    OperatorResult = Operator();
                }

                // Calculate the new average amount of states expanded per local optimum.
                RRHC_States_Avg = (RRHC_States_Avg * RRHC_Restarts + RRHC_States_Local) / (RRHC_Restarts + 1);

                // Check whether it was a local optimum or the solution.
                int FullEvaluation = FullEvaluate();
                if ( FullEvaluation == 0 )
                {
                    RRHC_OptFound = true;
                    return;
                }
                // If we found a local optimum, start over.
                else
                {
                    RRHC_Restarts++;
                    RRHC_Best_So_Far = FullEvaluation;

                    ReadSudoku();
                    FillSudoku();
                }
            }

            RRHC_OptFound = false;
        }

        static void ILS()
        {

        }

        static void TabuSearch()
        {

        }

        static bool Operator()
        {
            int eval1, eval2, tmp;

            for ( int i = 0; i < Nsq; i++ )
            {
                // To be able to switch, we need 2 or more non-fixated numbers in the block.
                if ( OpenIdxPerBlock[ i ].Count < 2 )
                    continue;

                for ( int j = 0; j < OpenIdxPerBlock[ i ].Count; j++ )
                {
                    for ( int k = 0; k < OpenIdxPerBlock[ i ].Count; k++ )
                    {
                        if ( j == k )
                            continue;

                        eval1 = Evaluate( OpenIdxPerBlock[ i ][ k ], OpenIdxPerBlock[ i ][ j ] );

                        // Rows and columns already perfect, no switching.
                        if ( eval1 == 0 )
                            continue;

                        tmp = Sudoku[ OpenIdxPerBlock[ i ][ j ] ];
                        Sudoku[ OpenIdxPerBlock[ i ][ j ] ] = Sudoku[ OpenIdxPerBlock[ i ][ k ] ];
                        Sudoku[ OpenIdxPerBlock[ i ][ k ] ] = tmp;

                        eval2 = Evaluate( OpenIdxPerBlock[ i ][ k ], OpenIdxPerBlock[ i ][ j ] );

                        // No improvement made, undo change.
                        if ( eval1 <= eval2 )
                        {
                            tmp = Sudoku[ OpenIdxPerBlock[ i ][ j ] ];
                            Sudoku[ OpenIdxPerBlock[ i ][ j ] ] = Sudoku[ OpenIdxPerBlock[ i ][ k ] ];
                            Sudoku[ OpenIdxPerBlock[ i ][ k ] ] = tmp;
                        }
                        // Improvement made, method done.
                        else
                            return true;
                    }
                }
            }
            // No changes made, Sudoku is complete or local optimum found.
            return false;
        }

        static int Evaluate( int idx1, int idx2 )
        {
            int result = 0;

            for ( int i = 0; i < Nsq; i++ )
            {
                if ( !GetRow( idx1 ).Contains( i ) )
                    result++;
                if ( !GetCol( idx1 ).Contains( i ) )
                    result++;
                if ( !GetRow( idx2 ).Contains( i ) )
                    result++;
                if ( !GetCol( idx2 ).Contains( i ) )
                    result++;
            }
            return result;

            return -1;
        }

        static int FullEvaluate()
        {
            int result = 0;

            for ( int i = 0; i < Nsq; i++ )
                for ( int j = 0; j < Nsq; j++ )
                {
                    if ( !GetRow( j ).Contains( i ) )
                        result++;

                    if ( !GetCol( j ).Contains( i ) )
                        result++;
                }

            return result;
        }

        static int[] GetBlockIndices( int which )
        {
            int[] result = new int[ Nsq ];
            // Take all the rows that form the block, and with the correct offset, write the relevant spaces to the result.            
            int offset = (which % N) * N;

            for ( int j = 0; j < N; j++ )
            {
                int row = j + (which / N) * N;
                for ( int i = 0; i < N; i++ )
                    result[ i + (j * N) ] = i + row * Nsq + offset;
            }
            return result;
        }

        static int[] GetRow( int which )
        {
            int[] result = new int[ Nsq ];
            which %= Nsq;
            for ( int i = 0; i < Nsq; i++ )
                result[ i ] = Sudoku[ which * Nsq + i ];

            return result;
        }

        static int[] GetCol( int which )
        {
            int[] result = new int[ Nsq ];
            which /= Nsq;
            for ( int i = 0; i < Nsq; i++ )
                result[ i ] = Sudoku[ i * Nsq + which ];

            return result;
        }

        static void ReadSudoku()
        {
            // Make REALLY sure it's empty
            Sudoku = null;
            Sudoku = new int[ Nsq * Nsq ];

            Sudoku = ParseTxtToArray( SudokuPath );
        }

        static void FillSudoku()
        {
            // Fill each block with a permutation of the missing numbers
            for ( int i = 0; i < Nsq; i++ )
            {
                Shuffle<int>( OpenNumsPerBlock[ i ] );

                for ( int j = 0; j < OpenNumsPerBlock[ i ].Count(); j++ )
                    Sudoku[ OpenIdxPerBlock[ i ][ j ] ] = OpenNumsPerBlock[ i ][ j ];
            }
        }

        static void Shuffle<T>( IList<T> list )
        {
            int n = list.Count;
            while ( n > 1 )
            {
                n--;
                int k = rng.Next( n + 1 );
                T value = list[ k ];
                list[ k ] = list[ n ];
                list[ n ] = value;
            }
        }

        static void Print()
        {
            for ( int i = 0; i < Nsq; i++ )
            {
                for ( int j = 0; j < Nsq; j++ )
                {
                    if ( Sudoku[ i * Nsq + j ] == 0 )
                        Console.Write( "." );
                    else
                        Console.Write( Sudoku[ i * Nsq + j ] );
                }
                Console.Write( "\n" );
            }
            Console.WriteLine( "" );
        }

        static void PrintRRHCResults()
        {
            Console.WriteLine( "Have we found the solution?\n{0}", RRHC_OptFound );
            Console.WriteLine( "How many Restarts did it take?\n{0}", RRHC_Restarts );
            Console.WriteLine( "How many States dId we expand?\n{0}", RRHC_States_Global );
            Console.WriteLine( "HoE maMy States did wE expand per Restart?\n{0}", RRHC_States_Local_Avg );
        }

        static int[] ParseTxtToArray( string Path )
        {
            string[] totString = new string[ Nsq * Nsq ];
            StreamReader sr = new StreamReader( Path );
            String read = sr.ReadLine();
            String[] sa;
            int offset = 0;
            while ( read != null && read != "" )
            {
                sa = read.Split( ' ' );

                for ( int j = 0; j < Nsq; j++ )
                    totString[ offset + j ] = sa[ j ];

                read = sr.ReadLine();
                offset += Nsq;
            }

            int[] result = new int[ Nsq * Nsq ];

            // Traverse the entire string, adding the chars to the array in int form.
            for ( int i = 0; i < Nsq * Nsq; i++ )
            {
                // An empty space
                if ( totString[ i ] == "." )
                    result[ i ] = 0;

                // A non-empty space
                else
                    result[ i ] = int.Parse( totString[ i ] );
            }

            return result;
        }
    }
}


