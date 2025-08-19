Package: qtwebengine[core,geolocation,pdf,spellchecker,webchannel,webengine]:x64-windows@6.8.3#2

**Host Environment**

- Host: x64-windows
- Compiler: MSVC 19.43.34809.0
-    vcpkg-tool version: 2025-07-21-d4b65a2b83ae6c3526acd1c6f3b51aff2a884533
    vcpkg-scripts version: 8cce1e1118 2025-08-05 (9 hours ago)

**To Reproduce**

`vcpkg install `

**Failure logs**

```
-- Using cached win_flex_bison-2.5.24.zip
Downloading https://files.pythonhosted.org/packages/6c/dd/a834df6482147d48e225a49515aabc28974ad5a4ca3215c18a882565b028/html5lib-1.1-py2.py3-none-any.whl -> html5lib-1.1-py2.py3-none-any.whl
Successfully downloaded html5lib-1.1-py2.py3-none-any.whl
Downloading https://files.pythonhosted.org/packages/b7/ce/149a00dd41f10bc29e5921b496af8b574d8413afcd5e30dfa0ed46c2cc5e/six-1.17.0-py2.py3-none-any.whl -> six-1.17.0-py2.py3-none-any.whl
Successfully downloaded six-1.17.0-py2.py3-none-any.whl
Downloading https://files.pythonhosted.org/packages/f4/24/2a3e3df732393fed8b3ebf2ec078f05546de641fe1b667ee316ec1dcf3b7/webencodings-0.5.1-py2.py3-none-any.whl -> webencodings-0.5.1-py2.py3-none-any.whl
Successfully downloaded webencodings-0.5.1-py2.py3-none-any.whl
Downloading https://github.com/pypa/get-pip/archive/24.2.tar.gz -> pypa-get-pip-24.2.tar.gz
Successfully downloaded pypa-get-pip-24.2.tar.gz
-- Extracting source C:/vcpkg/downloads/pypa-get-pip-24.2.tar.gz
-- Using source at C:/vcpkg/buildtrees/qtwebengine/src/24.2-36e81c02c7.clean
-- Setting up python virtual environment...
-- Installing python packages: --no-index;C:/vcpkg/downloads/html5lib-1.1-py2.py3-none-any.whl;C:/vcpkg/downloads/six-1.17.0-py2.py3-none-any.whl;C:/vcpkg/downloads/webencodings-0.5.1-py2.py3-none-any.whl
-- Setting up python virtual environment... finished.
CMake Warning at buildtrees/versioning_/versions/qtwebengine/d6ba2c93ca1df0a2b8d4025aa97e9539ba99bfa1/portfile.cmake:188 (message):
  Buildtree path 'C:/vcpkg/buildtrees/qtwebengine' is too long.

  Consider passing --x-buildtrees-root=<shortpath> to vcpkg!

  Trying to use 'C:/vcpkg/buildtrees/qtwebengine/../tmp'
Call Stack (most recent call first):
  scripts/ports.cmake:206 (include)


CMake Error at buildtrees/versioning_/versions/qtwebengine/d6ba2c93ca1df0a2b8d4025aa97e9539ba99bfa1/portfile.cmake:193 (message):
  Buildtree path is too long.  Build will fail! Pass
  --x-buildtrees-root=<shortpath> to vcpkg!
Call Stack (most recent call first):
  scripts/ports.cmake:206 (include)



```

**Additional context**

<details><summary>vcpkg.json</summary>

```
{
  "name": "pokertracker2",
  "version": "1.0.0",
  "description": "A professional desktop poker session tracker application",
  "homepage": "https://github.com/yourusername/pokertracker2",
  "dependencies": [
    {
      "name": "qt",
      "version>=": "6.8.0"
    }
  ],
  "builtin-baseline": "8cce1e1118ea2569d5117f096035671a8490b8f4",
  "overrides": [
    {
      "name": "qt",
      "version": "6.8.3"
    }
  ]
}

```
</details>
