# SecureServer
A server for serving encrypted files

## Why tho?
Ever wanted to deploy something private on a public server? Now you can as all files are stored encrypted in the servers folder and are thus gibberish to everyone who doesn't have the decryption key

## How do I use it
On your private machine put all files you want encrypted into `frontend/decrypted`, specify a key in the key variable of SecureServer.cs and start the server. All files will be stored encrypted in the folder `frontend/encrypted`.

Then deploy the server on the remote machine **WITHOUT** the decrypted folder.
