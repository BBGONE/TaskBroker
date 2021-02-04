# Sql Server Service Broker External Activation Message Handler

A generic host based .NET core app which can be run as a Windows Service.

## Building and creating a service

### Build and publish

dotnet publish --configuration Release

### Define the service

sc create TaskBrokerService binPath= "c:\DATA\DEVELOP\TaskBrokerCore3\TaskBroker\bin\Release\netcoreapp2.2\win7-x64\publish\TaskBroker.exe"

### Start the service

sc start TaskBrokerService

For further details see the blog post [Running a .NET Core Generic Host App as a Windows Service](https://www.stevejgordon.co.uk/running-net-core-generic-host-applications-as-a-windows-service)

### Why this thing is needed

External activation helps to execute external tasks (written in C#) from inside of Sql Server Stored Procedures. 
The benefits that the tasks (executors) are executed outside of Sql Server process and implemented in more robust language than Transact SQL. 
For example, i use it to write stored procedures for some Reports (in SQL Server Reporting Services)  - when the report is executed the stored procedure triggers execution
of a task and waits for its completion. When the execution of the task is completed, the report is created based on the data obtained from the execution of that task.
There are more possibilities (Application to Application communication), but for them, it is more convenient to use other Message Buses (like - NServiceBus, MassTransit or Rebus). 
And this thing is better for SQL Server - Outside World communication (send emails, import data, etc).
It can also be used as a relay to send mesages further to a different message broker like Kafka or Rabbit MQ.

### Why  other Message Buses rarely use Sql Server Service Broker (SSSB) Transport

It is difficult to plug SSSB transport into existing message buses, because SSSB message handling has its peculiar features
which need special message processing infrastructure. So it is better to use specific application for this task, than
to try to squeeze it into existing applications.

