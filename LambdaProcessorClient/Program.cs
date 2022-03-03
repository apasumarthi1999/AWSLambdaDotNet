using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Microsoft.Extensions.Configuration;

namespace LambdaProcessorClient
{
   class Program
   {
      static ConcurrentBag<int> s_primeNumbers = new ConcurrentBag<int>();
      static int s_maxConcurrentExecutions = 0;
      static int s_concurrentExecutions = 0;
      static int s_executions = 0;

      static void Main( string[] args )
      {
         var configBuilder = new ConfigurationBuilder().AddJsonFile( "appsettings.json" );
         var config = configBuilder.Build();

         Amazon.Lambda.AmazonLambdaClient lambdaClient = new Amazon.Lambda.AmazonLambdaClient(
            config["LambdaApiKey"],
            config["LambdaApiSecret"],
            RegionEndpoint.GetBySystemName( config["LambdaRegion"] ) );

         Stopwatch watch = Stopwatch.StartNew();

         // Parallelize the prime number verification, as each number can be verified independently
         Parallel.For( 2, 1001, ( number ) =>
         {
            // We want to wait until all the numbers are verified in the parallel loop.
            // However, we cannot use async C# lambda inside a parallel loop
            // (we can use async/await, but the loop returns before completion of the lambda functions, which we do not want).
            // Hence, we have to wait on the completion of the prime number execution method using its Task awaiter.

            IsPrimeAsync( number, lambdaClient ).GetAwaiter().GetResult();

         } );

         Console.WriteLine( $"Elapsed time {watch.ElapsedMilliseconds} ms. Prime Number Count {s_primeNumbers.Count}" );

         foreach ( var number in s_primeNumbers )
            Console.WriteLine( number );
      }

      static async Task IsPrimeAsync( int number, Amazon.Lambda.AmazonLambdaClient lambdaClient )
      {
         // Update concurrent executions and max concurrent executions.
         // Only for analysis purpose.
         var ce = Interlocked.Increment( ref s_concurrentExecutions );

         // Not accurate as this is not thread safe assignment. But we just want to roughly understand the max concurrency.
         s_maxConcurrentExecutions = Math.Max( ce, s_maxConcurrentExecutions );

         Console.WriteLine( $"Max Concurrent Executions {s_maxConcurrentExecutions}, Concurrent Executions {ce}" );

         // Invoke the Lambda in request/response format, so that the call awaits until the
         // lambda returns a response.
         var response = await lambdaClient.InvokeAsync( new Amazon.Lambda.Model.InvokeRequest()
         {
            FunctionName = "LambdaProcessor",
            InvocationType = Amazon.Lambda.InvocationType.RequestResponse,
            Payload = number.ToString()
         } );

         // Parse the response and add to our prime numbers list if the number is a prime number.
         var result = bool.Parse( new StreamReader( response.Payload ).ReadToEnd() );

         if ( result )
            s_primeNumbers.Add( number );

         // Update total executions.
         // Only for anlaysis purpose.
         Interlocked.Decrement( ref s_concurrentExecutions );
         int executions = Interlocked.Increment( ref s_executions );

         Console.WriteLine( $"Total Executions {executions}" );
      }
   }
}
