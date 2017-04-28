#
# GenerateTestData.ps1
#

# Verify source tests artifacts are available and have not changed.
# Use of git could be slow but why not test this
# Each file should have a sha512 hash available in test data config file
# If data are generated and data/source file(s) hash(es) are in sync with config
# we are good to go with test run on current data set
#
# If data are not available or not in sync regenerate them and update config
#



