redundancy-windows-client
=========================

The windows client program for Redundancy servers.
WARNING: The github version isn't working properly yet! Use it only for experimental application.

Bilateral synchronization is now implemented (sync from server to client and vice versa).

Requirements
------------
- This client requires .NET 2.0 or an equivalent Mono version to run.
- The programme folder must contain the HTTPPostRequestLib4 assembly. You can download it here: http://download.zonicom.de/HttpPostRequestLib4.rar
- You need to create a file named appConfig.xml in the programme folder with following content:
  ```
  <config>
    <apiUri>https://www.mySite.com/Demo/Includes/API/api.inc.php</apiUri><!-- URI to API script -->
    <syncPath>Sync</syncPath><!--Path of Sync folder (absolute or relative to programme folder path-->
  </config>
  ```
