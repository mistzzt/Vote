# Vote for TShock

投票插件基于TShock，用途是让玩家投票。

有了这个插件，玩家可以投票封禁、驱逐、禁言或杀死某玩家；而且玩家可以投票执行指令。

**注意：要用这个插件，玩家需要登录。**

## 要求：
- API版本: 2.0
- TShock版本: 4.3.22

## 指令：
- `/vote <ban/kick/mute/kill> <player>` - *Starts a new vote on a player.*
- `/vote <command line>` - *Starts a new vote on execting a command.*
- `/vote help [page]` - *Gets a list of helps.*
- `/reason <reason>` - *Adds your reason to the vote you started.*
- `/assent [player]` - *Votes for a player.*
- `/dissent [player]` - *Votes against a player.*
    - `/y` - *Confirms your vote.*
    - `/n` - *Cancel your vote.*

### **e.g.** How to start a new vote on killing mist
1. `/vote kill mist` // System: **Vote will be started after using */reason \<Reason\>***
2. `/reason test-for-me` // System: **Vote: *Killing mist* for *test-for-me* by *mistwang* started.**
3. Now, use `/assent` or `/dissent` to cast votes. // System: **Really vote for/against *killing mist*? Use `/y` to confirm, `/n` to cancel.**
4. Use `/y` to confirm your answer.

### **e.g.** How to start a new vote to executing commands
1. `/vote /user del mist` // System: **Vote will be started after using */reason \<Reason\>***
2. `/reason test-for-me` // System: **Vote: */user del mist* for *test-for-me* by *mistwang* started.**
3. Now, use `/assent` or `/dissent` to cast votes. // System: **Really vote for/against */user del mist*? Use `/y` to confirm, `/n` to cancel.**
4. Use `/y` to confirm your answer.

You can also find this plugin in [TShock offical forum][tshockco].

## 权限
- `vote.player.startvote` - *User can start a new vote.*
- `vote.player.vote` - *User can vote for/against someone.*

## How does this plugin work
This plugin will use a server-player `wheel` in group `wheel` to execute commands.

Group `wheel` will be created the first time you use this plugin. It will has these permissions: `Permissions.ban, Permissions.kick, Permissions.mute, Permissions.kill`

You can also change the group to another such as trustedadmin, but you should never change the group to superadmin, as players may execute some destructive commands.

## 配置文件：**`vote.json`**
```
{
  "ExecutiveGroup": "wheel", // The group which execute commands.
  "MaxAwaitingVotingTime": 60, // The max time a vote will last.
  "MaxAwaitingReasonTime": 30, // The max time for player to give his reason.
  "ShowResult": true // Whether system will show vote-result of players.
}
```

   [tshockco]: <https://tshock.co>
