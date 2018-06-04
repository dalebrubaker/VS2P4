namespace BruSoft.VS2P4
{
    /// <summary>
    /// The possible states of a file in Perforce. See http://filehost.perforce.com/perforce/r09.1/doc/help/p4v-html-pure/About_file_icons.html
    /// </summary>
    public enum FileState
    {
        NotSet,                                                                         // STATEICON_EXCLUDEDFROMSCC            Solid Red circle with white dash
        NotInPerforce,                                                                  // STATEICON_BLANK                      No icon 
        OpenForEdit, // checked out                                                     // STATEICON_CHECKEDOUT                 Red check mark
        OpenForEditOtherUser, // checked out by other user                              // CUSTOMICON_CHECKEDIN                 Page with green dot in lower right  corner
        OpenForEditDiffers, // Differs and also OpenForEdit                             // CUSTOMICON_DIFFERS                   Yellow triangle
        Locked, // Locked implies also OpenForEdit                                      // STATEICON_READONLY                   Padlock black outline
        LockedByOtherUser,                                                              // CUSTOMICON_CHECKEDIN                 Page with green dot in lower right  corner
        OpenForDelete,                                                                  // STATEICON_DISABLED                   Red circle with diagonal (Shouldn't see this in a solution)
        OpenForDeleteOtherUser,                                                         // STATEICON_DISABLED                   Red circle with diagonal (Shouldn't see this in a solution)
        DeletedAtHeadRevision,                                                          // STATEICON_DISABLED                   Red circle with diagonal (Shouldn't see this in a solution)
        OpenForAdd,                                                                     // CUSTOMICON_ADD                       Red plus
        OpenForRenameSource, // the new name                                            // STATEICON_DISABLED                   Red circle with diagonal (Shouldn't see this in a solution)
        OpenForRenameTarget, // the old name                                            // CUSTOMICON_ADD                       Red plus
        CheckedInHeadRevision, // synced to head revision                               // CUSTOMICON_CHECKEDIN                 Page with green dot in lower right  corner
        CheckedInPreviousRevision, // synced to previous revision                       // CUSTOMICON_DIFFERS                   Yellow triangle

        // Below have not been tested
        NeedsResolved, // NeedsResolved implies also OpenForEdit or OpenForIntegrate    // CUSTOMICON_RESOLVE                   Red question mark
        OpenForBranch,                                                                  // STATEICON_ORPHANED                   Blue flag
        OpenForIntegrate, // will need resolve                                          // CUSTOMICON_RESOLVE                   Red question mark

        //OpenForAddOtherWorkspace, Doesn't seem to mean anything
    }
}

// VsStateIcon
// Member name	                        Description
// STATEICON_NOSTATEICON	            Not supported.                                                  No icon
// STATEICON_CHECKEDIN	                Item is checked in.                                             Padlock solid blue 
// STATEICON_CHECKEDOUT	                Item is checked out.                                            Red check mark
// STATEICON_ORPHANED	                Item is orphaned.                                               Blue flag
// STATEICON_EDITABLE	                Item is editable.                                               Pencil
// STATEICON_BLANK	                    Blank Icon.                                                     No icon
// STATEICON_READONLY	                Item is read only.                                              Padlock black outline
// STATEICON_DISABLED	                Item is disabled.                                               Red circle with diagonal
// STATEICON_CHECKEDOUTEXCLUSIVE	    Item is checked-out exclusively by user.                        Red check mark, same as CHECKEDOUT
// STATEICON_CHECKEDOUTSHAREDOTHER	    Item is checked-out shared by someone else.                     Blue user
// STATEICON_CHECKEDOUTEXCLUSIVEOTHER	Item is checked-out exclusively by someone else.                Blue user, same as CHECKEDOUTSHAREDOTHER
// STATEICON_EXCLUDEDFROMSCC	        Item is excluded from source code control.                      Solid Red circle with white dash
// STATEICON_MAXINDEX	                Flag to indicate highest value used in the enumeration.         No icon

// Custom Icons
// Limit 4 per http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.shell.interop.ivssccglyphs.getcustomglyphlist(VS.80).aspx
// CUSTOMICON_CHECKEDIN Page with green dot in lower left corner
// CUSTOMICON_ADD       Red plus
// CUSTOMICON_RESOLVE   Red question mark
// CUSTOMICON_DIFFERS   Yellow triangle
