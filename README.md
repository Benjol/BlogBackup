#BlogBackup project
Uses Blogger exported xml to backup all blog images to local disk

##What's it for?
Use this to backup all the images from your Blogger blog.

##How? 
1. Hit the download link (above right), and choose one of the two zip files to download (see Prerequisites below to work out which)
2. There is no installer, the exe inside the zip is stand-alone
3. Go to the Settings/Other tab in your Blogger management interface, and hit the 'Export blog' link - save to disk
4. Open a command prompt* (Start > Run > cmd) and navigate to the folder where you saved BlogBackup.exe
5. Type "blogbackup [path to blog xml] [path to save images to]" and hit enter
6. The program will download all images, printing status to the console
7. At the end, a file (blogbackuplog.txt) is saved to the path, consult this to see if any downloads timed out: if so you may want to run the program again

*If you're not comfortable with using the command prompt, you can create a 'batch' file next to BlogBackup.exe, and paste into it the command shown above in step 5 (with the approprite paths)

##Prerequisites
You need to have at least .Net framework 2.0 (http://www.microsoft.com/download/en/details.aspx?id=19) installed.
If you already have the F# runtime installed (http://www.microsoft.com/download/en/details.aspx?id=13450), you can download the 'vanilla' BlogBackup.zip file. Otherwise you need to download the BlogBackupStandalone.zip file (which has the necessary F# components inside)

##Can be used elsewhere
Though I wrote this specifically for downloading from Blogger, it can be used for downloading any images from anywhere, based on links to images in a text file.

##Why doesn't it download the blogger xml for me?
Because I am philosophically opposed to the idea of people trusting a program that they just downloaded off the internet with their blogger login credentials.

##Future work
The next two steps (I'm not sure in which order) are to enable downloading embedded videos (not easy), and to recreate the html on disk.

##License terms
Use this software - compiled or source code - any which way you want, but don't blame me if it does something you don't like.

##Changelog

###02 Marche 2012
Initial version