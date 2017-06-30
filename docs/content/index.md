XrmMockup
======================


<div class="row-fluid">
  <div class="span1"></div>
  <div class="span8">
    <div class="well well-small" id="nuget">
      The XrmMockup executable can be 
      <a href="https://nuget.org/packages/Delegate.XrmMockup">
        installed from NuGet
      </a>:
      <pre>PM> Install-Package Delegate.XrmMockup</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>


What is it?
-----------
XrmMockup is a tool for mocking your own MS Dynamics CRM instance. Instead of testing against specific parts of your code, thus requiring you to define
how behavior is mocked, XrmMockup acts like a complete CRM system, which you send commands to. This enables you to test and debug the functionality of your CRM system,
with all your plugins and workflows automatically running inside your test. 
Some of XrmMockup's features include:

* Support for executing requests via an organization service to XrmMockup.
* Automatic execution of plugins and workflows during requests, and the chains these process create.
* Test waiting workflows by increasing the current time of the XrmMockup instance.
* Test functionality based upon which security roles teams or users have.

Getting started
---------------
First read how to [setup XrmMockup](setup.html), then see [how to use XrmMockup](usingmockup.html).