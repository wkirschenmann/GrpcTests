# GrpcTests

Different tests of grpc server and client in dotnet. 
The tests were written as part of an attempt to reproduce
ENHENCE_YOUR_CALM errors obtained in another application.

The class [`GrpcTests.ClientTest.TestBase`](ClientTest\TestBase.cs) 
contains a `TestDuration` property that can be used to modify
the durations of the tests. A value bigger than 2 minutes and 30
seconds is recommended since it corresponds to the default value 
of some network timeouts.

All tests are written and described in this file. The tests are executed by two other 
classes inheriting from `GrpcTests.ClientTest.TestBase`:

* `GrpcTests.ClientTest.SingleProcess` launches both the GRPC server
  and client inside the same process. This test class uses Unix 
  Domain Socket (or their Windows equivalent) to transport the data.
* `GrpcTests.ClientTest.SubProcessServer` launches the GRPC server 
  in a subprocess. This test class uses Unix Domain Socket (or 
  their Windows equivalent) to transport the data.






