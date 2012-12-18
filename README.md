== SPDYCheck ==

SPDYCheck allows you to audit a web server for SPDY support, and help troubleshoot any problems that arise. It also tests for some best practices to best utilize SPDY, such as automatically redirecting from HTTP to SSL/SPDY.

SPDYCheck to consists of two projects: SPDYAnalysis, and the SPDYCheck.org website.

The *SPDYAnalysis* project contains the code which does the testing for SPDY support. The `SPDYChecker` class is where the guts are and it contains one static method `SPDYChecker.Test()`. This returns a `SPDYResult` object, which has some strings and boolean flags indicating how much or how little SPDY infrastructure a host supports. The rest of the code is support structure stuff, to get HTTP headers, to inspect X.509 certificate errors, etc. They can be safely ignored.

The *SPDYCheck.org* project contains the website code. This consists of an HTML page to act as the interface. Ajax is used to communicated back with a service, which pretty much just calls `SPDYChecker.Test()` and JSON-ifies the `SPDYResult` object. JavaScript in the HTML parses the results and presents the info to the user.

SPDYCheck is open source software released under the GPL. Zoompf's logo is the copyright of Zoompf Incorporated.