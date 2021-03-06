using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer( typeof( Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer ) )]

namespace AWSLambdaProcessorDotNet
{
   public class Function
   {

      /// <summary>
      /// A simple function that takes a number as input and returns true if the number is prime, else returns false
      /// </summary>
      /// <param name="number"></param>
      /// <param name="context"></param>
      /// <returns>True (if prime)/False (if not prime)</returns>
      public bool FunctionHandler( int number, ILambdaContext context )
      {
         if ( number == 2 )
            return true;

         if ( number % 2 == 0 )
            return false;

         var sqrtNumber = Math.Ceiling( Math.Sqrt( number ) );

         for ( int i = 3; i <= sqrtNumber; i++ )
            if ( number % i == 0 )
               return false;

         return true;
      }
   }
}
