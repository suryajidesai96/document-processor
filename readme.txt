# Manual install

## Install Word 2007 (or higher)

Office 365 version of Word works fine also.

## Configure Word Automation

Configure to Word Automation to run with the interactive identity and allow NETWORK SERVICE user to run COM components.

**Note: This is now done via Deploy script**

1. Run the component services configuration tool in 32 bit mode using ```mmc comexp.msc /32``` 
2. Find the Component Entry for Microsoft Word 2007-2013 Document
3. Right click, properties, Identity Tab
4. Under "What user account do you want to use to run this application" set to Interactive User.
5. Under the _Security_ tab, within Launch and Activation Permissions, add the plusuat and NETWORK SERVICE user with _Local Launch_ and _Local Activation_ permissions.
6. Under the _Security_ tab, within Access Permissions, add the plusuat and NETWORK SERVICE user with _Local Access_ permissions.

## Errors

> Unable to start a DCOM Server: {000209FF-0000-0000-C000-000000000046}. The error:
"19"

Make sure COM component settings within component services are setup as above.