open System
open System.IO
open System.Xml

//Helper function
let rootPath = @"D:\My Documents\My Dropbox\Dev\BlogBackup\"
let urltopath (url:string) = Path.Combine(rootPath, url.Replace("http://", ""))

//Get file
let filePath = rootPath + "blog-03-30-2012.xml"
let xdoc = new XmlDocument()
let xml = File.ReadAllText(filePath).Replace(@"xmlns='http://www.w3.org/2005/Atom' ","")
xdoc.LoadXml(xml)

//Get template
let template = xdoc.SelectSingleNode("//feed/entry[contains(id, 'template')]/content/text()").Value
File.WriteAllText(rootPath + "Template.xml", template)

//Get posts
let posts = xdoc.SelectNodes("//feed/entry[category[contains(@term,'kind#post')]]")

//Get comments for posts
let comments = xdoc.SelectNodes("//feed/entry[category[contains(@term,'kind#comment')]]")

//work out how to get comments for a given post
//what file name for post?
//Decide how to handle archives!
//Decide how to handle relative paths

//stuff to get for filling template:
// BlogPageTitle, BlogMetaData, BlogURL, BlogTitle, BlogDescription, BlogDateHeaderDate
// per post: BlogItemNumber, BlogItemUrl, BlogItemTitle, BlogItemBody, BlogItemAuthorNickname, 
//           BlogItemPermalinkUrl, BlogItemDateTime, BlogItemControl, BlogItemCreate
// comments: BlogItemCommentCount, BlogCommentNumber, BlogCommentAuthor, BlogCommentBody, BlogCommentDateTime, BlogCommentDeleteIcon
// other:  NewerPosts, OlderPosts, BlogMemberProfile
// previous items: BlogItemPermalinkURL, BlogPreviousItemTitle
// archive: BlogArchiveURL, BlogArchiveName

//replace 'fixed' bits in template
//BlogPageTitle & BlogTitle = "//feed/title/text()"
//BlogMetaData = ignore?, BlogURL = "//feed/link[@rel='alternate']/@href"
//BlogDescription = "//feed/entry[contains(id, 'BLOG_DESCRIPTION')]/content/text()"

//for each post, create a page (with comments in)
// BlogDateHeaderDate = entry:"published/text()"
// BlogItemNumber = entry:"id/text()".(.*(\d+)$)
// BlogItemUrl = not used?
// BlogItemTitle = entry:"title/text()"
// BlogItemBody = entry:"content/text()"
// BlogItemAuthorNickname = entry:"author/name/text()"
// BlogItemPermalinkUrl = need to decide, or use entry:link[@rel='alternate']/@href (note year/month)
// BlogItemDateTime = entry:"published/text()"
// BlogItemControl, BlogItemCreate = ignore
// BlogItemCommentCount = comments:"thr:in-reply-to[@ref='" + postid + "']".count
// BlogCommentNumber = comment:"id/text()".(.*(\d+)$)
// BlogCommentAuthor = comment: "author/name/text()"
// BlogCommentBody = comment:"content/text()"
// BlogCommentDateTime =  comment:"published/text()"
// BlogCommentDeleteIcon = ignore
// NewerPosts/OlderPosts

// for each archive duration, create a page (without comments)
// are at root with format yyyy_mm_01_archive.html
//link alternat has url