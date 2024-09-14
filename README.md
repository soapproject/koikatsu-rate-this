# koikatsu-rate-this

This project is a plugin for Koikatsu that allows users to rate character cards directly from the character creation screen (Maker). The plugin adds a "Rate It" checkbox in the right-sidebar. Once toggled, a panel will appear with configurable rating buttons. Based on the selected rating, the character card will be automatically moved to a designated folder under \Koikatsu\UserData\chara\female\{RatingType}.

Key features:

Customizable Rating Categories: Users can define different rating types in the plugin configuration (e.g., "Like", "Dislike", "MissingMod", "OtherIssue").  

Character Management: Sort characters by rating, and move them to the appropriate folders.  

Buffer System: Ratings can be assigned to multiple characters and batch-processed in one go.  

This tool is ideal for users who wish to quickly organize their character cards based on personal preferences or issues found in the cards.

![image](https://github.com/user-attachments/assets/21b3078e-2b40-4298-90e3-1b8f1674820f)

![image](https://github.com/user-attachments/assets/bf48db3f-3a43-4031-8110-b70a8df1c6a6)

## Config

The plugin allows users to customize rating categories by specifying folder names in the configuration file. These folders are where the character cards will be moved to based on the selected rating. You can define multiple rating categories by separating them with commas.

For example:
```
Like, Dislike, MissingMod, OtherIssue
```
![image](https://github.com/user-attachments/assets/910a2bde-6e2c-434a-bde1-c15e718f2d23)

