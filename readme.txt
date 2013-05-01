
The project is structured as a Visual Studio 2010 solution (ScrewTurnWiki.sln).

In order to compile the application you can either build the solution in Visual Studio, or 
follow the instructions included in the Build directory.

In either case, you'll need the following components installed on your machine:


- Windows Azure Tools for Visual Studio 2010 (October 2012)
  http://www.microsoft.com/download/en/details.aspx?id=26940

- SQL Server Compact 4.0.8482.1
  http://www.microsoft.com/download/en/details.aspx?id=17876


To use the azure storage provider see the Web.Azure.config file under WebApplication folder.  

To configure the azure provider each plugin must have it's config="" filled in with an azure connection string.  An azure connection string is formatted as "DefaultEndpointsProtocol=http;AccountName=;AccountKey="
