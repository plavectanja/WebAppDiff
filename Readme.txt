WebAPI Diff

@Author: Tanja Plavec
@Date: 11.03.2022

******************
PUT Methods:
-PUT <host>/v1/diff/<ID>/left,
-PUT <host>/v1/diff/<ID>/right.

Endpoints accept JSON containing base64 encoded binary data.

Returned status:
- 201 Created: endpoint data is valid, diff object with <ID> is created/updated.
- 400 Bad Request: endpoint data is not valid (null or not base64 encoded binary data), diff object with <ID> is not created/updated.

******************
GET methods:
-GET <host>/v1/diff/<ID>.

Returned status:
- 200 OK: diff object with <ID> exists.
- 404 Not Found: diff object with <ID> does not exist.

If status OK is returned, then also data in JSON format are returned:
- diffResultType: "Equals", "ContentDoNotMatch", "SizeDoNotMatch",
- diffs: list of diffs in JSON format.

Diff data in JSON format:
- offset: first index of different substring
- length: length of different substring

*******************