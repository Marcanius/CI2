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
        static int N = 3, Nsq, RRHC_MaxRestarts = 10000, ILS_MaxRestarts = 10000;
        static int[] Sudoku;
        static Random rng;
        static List<int> startArray;
        static string SudokuPath = "C:\\Users\\matti\\Documents\\GitHub\\CI2\\TestSudokuVeryEz.txt";
        static List<int>[] OpenIdxPerBlock, ClosedNumsPerBlock, OpenNumsPerBlock;

        static bool RRHC_OptFound, ILS_OptFound;
        static int RRHC_Restarts, RRHC_States_Global, RRHC_States_Local, RRHC_States_Avg, RRHC_Best_So_Far = int.MaxValue;
        static int ILS_Restarts, ILS_States_Global, ILS_States_Local, ILS_States_Avg;
        static int ILS_Best = int.MaxValue;
        static Dictionary<int, int> ILS_Results;

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

            #region Random Restart
            // RandomRestart();
            //PrintRRHCResults();
            #endregion

            #region ILS
            ILS_Results = new Dictionary<int, int>();
            ILS_Results.Add( 2, int.MaxValue );
            ILS_Results.Add( 5, int.MaxValue );
            ILS_Results.Add( 10, int.MaxValue );
            ILS_Results.Add( 50, int.MaxValue );
            ILS_Results.Add( 100, int.MaxValue );
            ILS_Results.Add( 1000, int.MaxValue );
            ILS_Results.Add( 10000, int.MaxValue );

            List<int> keys = ILS_Results.Keys.ToList();
            foreach ( int key in keys )
                ILS( key );

            PrintILSResults();
            #endregion

            #region Tabu

            #endregion

            Console.ReadLine();
        }

        #region Algorithms

        static void RandomRestart()
        {
            bool OperatorResult;

            for ( int i = 0; i < RRHC_MaxRestarts; i++ )
            {
                OperatorResult = true;

                // Keep applying the operator until we have found an optimum
                while ( OperatorResult )
                {
                    RRHC_States_Global++;
                    RRHC_States_Local++;
                    OperatorResult = OperatorEval();
                }

                // Calculate the new average amount of states expanded per local optimum.
                RRHC_States_Avg = (RRHC_States_Avg * RRHC_Restarts + RRHC_States_Local) / (RRHC_Restarts + 1);

                // Check whether it was a local optimum or the solution.
                int FullEvaluation = FullEvaluate();
                RRHC_Best_So_Far = Math.Min( FullEvaluation, RRHC_Best_So_Far );

                if ( FullEvaluation == 0 )
                {
                    Print();
                    RRHC_OptFound = true;
                    return;
                }
                // If we found a local optimum, start over.
                else
                {
                    RRHC_Restarts++;
                    RRHC_States_Local = 0;
                    ReadSudoku();
                    FillSudoku();
                }
            }

            RRHC_OptFound = false;
        }

        static void ILS( int S )
        {
            bool opResult = true;
            int[] currentBest = new int[ Nsq * Nsq ];
            BackupSudoku( ref currentBest );

            for ( int i = 0; i < ILS_MaxRestarts; i++ )
            {
                // Do HillClimbing
                while ( opResult )
                {
                    ILS_States_Global++;
                    ILS_States_Local++;
                    opResult = OperatorEval();
                }

                // Check if this is the solution
                int FullEvaluation = FullEvaluate();
                if ( FullEvaluation == 0 )
                {
                    ILS_OptFound = true;
                    return;
                }
                else
                {
                    ILS_Results[ S ] = Math.Min( ILS_Results[ S ], FullEvaluation );
                    // Reset to the previous optimum, and try again
                    if ( FullEvaluation > ILS_Best )
                        RestoreBackup( ref currentBest );
                    // Backup the new best effort
                    else
                        BackupSudoku( ref currentBest );
                }

                // Apply the operator ILS_S more times, disregarding the evaluation function
                for ( int j = 0; j < S; j++ )
                    OperatorILS();
                ILS_Restarts++;
            }

            Console.WriteLine( "\nDone with S = {0}", S );
        }

        static void TabuSearch()
        {

        }

        #endregion

        #region Operators

        static bool OperatorEval()
        {
            int eval1, eval2;

            // Traverse each block.
            for ( int i = 0; i < Nsq; i++ )
            {
                // To be able to switch, we need 2 or more non-fixated numbers in the block.
                if ( OpenIdxPerBlock[ i ].Count < 2 )
                    continue;

                for ( int j = 0; j < OpenIdxPerBlock[ i ].Count; j++ )
                    for ( int k = 0; k < OpenIdxPerBlock[ i ].Count; k++ )
                    {
                        if ( j == k )
                            continue;

                        // Evaluate the current state
                        eval1 = Evaluate( OpenIdxPerBlock[ i ][ k ], OpenIdxPerBlock[ i ][ j ] );

                        // Rows and columns are already perfect, no switching.
                        if ( eval1 == 0 )
                            continue;

                        // Switch the two fields.
                        Switch( i, j, k );

                        // Evaluate the successor
                        eval2 = Evaluate( OpenIdxPerBlock[ i ][ k ], OpenIdxPerBlock[ i ][ j ] );

                        // No improvement made, undo change.
                        if ( eval1 <= eval2 )
                            Switch( i, j, k );
                        // Improvement made, method done.
                        else
                            return true;
                    }
            }

            // No changes made in the entire Sudoku, Sudoku is complete or local optimum found.
            return false;
        }

        static void OperatorILS()
        {
            int block = rng.Next( Nsq );
            int j = 0, k = 0;

            while ( j == k )
            {
                j = rng.Next( OpenIdxPerBlock[ block ].Count );
                k = rng.Next( OpenIdxPerBlock[ block ].Count );
            }

            Switch( block, j, k );

            //List<int> blocks = new List<int>();
            //for ( int i = 1; i <= Nsq; i++ )
            //    blocks.Add( i );

            //// Shuffle so we traverse the blocks in a random order.
            //Shuffle( blocks );

            //// Traverse each block.
            //for ( int blockid = 0; blockid < Nsq; blockid++ )
            //{
            //    int i = blocks[ blockid ];

            //    // To be able to switch, we need 2 or more non-fixated numbers in the block.
            //    if ( OpenIdxPerBlock[ i ].Count < 2 )
            //        continue;

            //    // For each available number, find a partner
            //    for ( int j = 0; j < OpenIdxPerBlock[ i ].Count; j++ )
            //        for ( int k = 0; k < OpenIdxPerBlock[ i ].Count; k++ )
            //        {
            //            if ( j == k )
            //                continue;

            //            // Switch the two fields.
            //            Switch( i, j, k );

            //            // We didn't check the evaluation, so we DEFINITELY made an improvement, surely.
            //            return true;
            //        }
            //}

            //// No changes made, Sudoku is complete or local optimum found.
            //return false;
        }

        static bool OperatorTabu()
        {
            return false;
            //int eval1 = int.MaxValue, eval2 = int.MaxValue;

            //for ( int i = 0; i < Nsq; i++ )
            //{
            //    // To be able to switch, we need 2 or more non-fixated numbers in the block.
            //    if ( OpenIdxPerBlock[ i ].Count < 2 )
            //        continue;

            //    for ( int j = 0; j < OpenIdxPerBlock[ i ].Count; j++ )
            //    {
            //        for ( int k = 0; k < OpenIdxPerBlock[ i ].Count; k++ )
            //        {
            //            if ( j == k )
            //                continue;

            //            if ( CheckEval )
            //            {
            //                eval1 = Evaluate( OpenIdxPerBlock[ i ][ k ], OpenIdxPerBlock[ i ][ j ] );

            //                // Rows and columns are already perfect, no switching.
            //                if ( eval1 == 0 )
            //                    continue;
            //            }

            //            // Switch the two fields.
            //            Switch( i, j, k );

            //            if ( CheckEval )
            //            {
            //                eval2 = Evaluate( OpenIdxPerBlock[ i ][ k ], OpenIdxPerBlock[ i ][ j ] );

            //                // No improvement made, undo change.
            //                if ( eval1 <= eval2 )
            //                    Switch( i, j, k );
            //                // Improvement made, method done.
            //                else
            //                    return true;
            //            }

            //            // We didn't check the evaluation, so we DEFINITELY made an improvement, surely.
            //            else
            //                return true;
            //        }
            //    }
            //}
            //// No changes made, Sudoku is complete or local optimum found.
            //return false;
        }

        #endregion

        #region Printing

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
            Console.WriteLine( "How many States did we expand?\n{0}", RRHC_States_Global );
            Console.WriteLine( "How many States did we expand per Restart?\n{0}", RRHC_States_Avg );
            Console.WriteLine( "How close did we get?\n{0}", RRHC_Best_So_Far );
        }

        static void PrintILSResults()
        {
            Console.WriteLine( "Solution?\n{0}", ILS_OptFound );
            foreach ( int key in ILS_Results.Keys )
                Console.WriteLine( "\nBest Score for S = {0}: {1}", key, ILS_Results[ key ] );
        }

        static void PrintTabuResults() { }

        #endregion

        #region Sudoku Functions

        static void Switch( int Block, int Idx1, int Idx2 )
        {
            List<int> openIdcs = OpenIdxPerBlock[ Block ];

            int tmp = Sudoku[ openIdcs[ Idx1 ] ];
            Sudoku[ openIdcs[ Idx1 ] ] = Sudoku[ openIdcs[ Idx2 ] ];
            Sudoku[ openIdcs[ Idx2 ] ] = tmp;
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

        static void BackupSudoku( ref int[] toBackupTo )
        {
            for ( int i = 0; i < Nsq * Nsq; i++ )
                toBackupTo[ i ] = Sudoku[ i ];
        }

        static void RestoreBackup( ref int[] toRestoreFrom )
        {
            for ( int i = 0; i < Nsq * Nsq; i++ )
                Sudoku[ i ] = toRestoreFrom[ i ];
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
        
        #endregion
    }
}