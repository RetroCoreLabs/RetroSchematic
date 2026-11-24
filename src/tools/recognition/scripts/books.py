"""The four ND-100 source books (READ-ONLY). Page numbers are 1-based PDF pages.

The books are kept outside the repository; set an environment variable to each PDF:
ND100_BINDER  "Binder 1x ND-100 diagrams" (ND library scan)
ND120_CPU     ND-120 CPU board 3202 drawings, rev D (in the nd-120 repository, DesignDocuments)
ND_SCSI       ND-350007-A1-EN "Schematic Circuit Diagrams" (SCSI controller 3207)
ND100_IOSYS   "nord100 - Input Output System" (drawings on pages 221-244)
"""
import os

BOOKS = {
 "binder": (os.environ.get("ND100_BINDER", ""), list(range(1, 158))),
 "nd120":  (os.environ.get("ND120_CPU", ""), list(range(1, 51))),
 "scsi":   (os.environ.get("ND_SCSI", ""), list(range(1, 25))),
 "iosys":  (os.environ.get("ND100_IOSYS", ""), list(range(221, 245))),
}
