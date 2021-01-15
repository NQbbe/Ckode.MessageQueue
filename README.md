# Ckode.MessageQueue
Helper library for Microsoft MessageQueue

In order to use MessageQueue with multicast, you need to enable both MessageQueue and its multicast support features in Windows.
After doing so you also need to bind multicast to your primary IP. Do this by executing this command in an administrative command prompt (replace YOURIP with the actual IP):

REG ADD "HKLM\SOFTWARE\Microsoft\MSMQ\Parameters" /v "MulticastBindIP" /t REG_SZ /d "YOURIP" /f 
