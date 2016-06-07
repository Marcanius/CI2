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
        static int N = 3, Nsq;
        static int[] Sudoku;
        static Random rng;
        static List<int> startArray;
        static string SudokuPath = "E:\\Documents\\Visual Studio 2015\\Projects\\CI 2\\CI 2\\TestSudokuVeryEz.txt";
        static List<int>[] OpenIdxPerBlock, ClosedNumsPerBlock, OpenNumsPerBlock;

        static void Main( string[] args )
        {
            Nsq = N * N;
            Sudoku = new int[ Nsq * Nsq ];
            rng = new Random();

            Sudoku = ParseTxtToArray( SudokuPath );

            OpenIdxPerBlock = new List<int>[ Nsq ];
            ClosedNumsPerBlock = new List<int>[ Nsq ];
            OpenNumsPerBlock = new List<int>[ Nsq ];

            Console.WriteLine( GetCol( 80 ) );

            //For each block
            for ( int i = 0; i < Nsq; i++ )
            {
                OpenIdxPerBlock[ i ] = new List<int>();
                ClosedNumsPerBlock[ i ] = new List<int>();
                OpenNumsPerBlock[ i ] = new List<int>();
                int[] blockIdx = GetBlockIndices( i );

                //For each digit
                for ( int j = 0; j < Nsq; j++ )
                {
                    if ( Sudoku[ blockIdx[ j ] ] == 0 )
                        OpenIdxPerBlock[ i ].Add( blockIdx[ j ] );
                    else
                        ClosedNumsPerBlock[ i ].Add( Sudoku[ blockIdx[ j ] ] );
                }

                for ( int j = 1; j <= Nsq; j++ )
                {
                    if ( !ClosedNumsPerBlock[ i ].Contains( j ) )
                        OpenNumsPerBlock[ i ].Add( j );
                }

                Shuffle<int>( OpenNumsPerBlock[ i ] );
                for ( int j = 0; j < OpenNumsPerBlock[ i ].Count(); j++ )
                {
                    Sudoku[ OpenIdxPerBlock[ i ][ j ] ] = OpenNumsPerBlock[ i ][ j ];
                }
            }
            //After this, the sudoku array has been initiated, every block contains numbers one through 9, having kept in mind the constraint of blocks, and without having moved the fixated spots.

            Print();
            Operator();
            Print();
            Operator();
            Print();
        }

        static void RandomRestart()
        {

        }

        static void ILS()
        {

        }

        static void TabuSearch()
        {

        }

        static void Operator()
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
                        if ( eval1 == 0 )
                            // Rows and columns already perfect, no switching.
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
                            return;
                    }
                }
            }
            //No changed made, Sudoku is complete or local optimum found.
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
                {
                    totString[ offset + j ] = sa[ j ];
                }

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


