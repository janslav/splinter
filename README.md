# splinter

Splinter is a unit test mutation analysis framework for .Net. Our aim is easy setup and useful output. Currently in early alpha, with a working prototype.

The introduction of mutation testing to a project should be as easy as just running the Splinter executable in a folder with test binaries.
Ultimately we plan to enable easy integration with existing CI / static analysis / code metrics reporting tools (TeamCity, SonarQube, Gallio, etc.)

If you'd like to download and play with the source code, please feel free. I also welcome offers of help and suggestions. Kick off a discussion here on CodePlex, or drop me a line.

Currently we only support MsTest. Support for further unit testing frameworks (such as nunit) should however be easy enough. Accepting pull requests :)

Our mutation code is based on NinjaTurtles.

The current status is alpha. We have a working prototype and are in need of volunteers who would try it out on their projects/test-suites and give us feedback.
We'd like to hear how much the results are in line with reasonable reality, how is performance, as well as what are the use cases and toolsets people wish to use this with.

The only output from the executable currently are its logs. We're still deciding on how to structure/render the final xml/html results.

JetBrains was kind enough to provide free Resharper Ultimate licences for this opensource project. Thanks!
