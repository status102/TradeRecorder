# Trade Recorder (原 Trade Buddy)
[![](https://img.shields.io/badge/dynamic/xml?color=success&label=repo%20version&query=%2F%2FProject%2FPropertyGroup%2FVersion&url=https%3A%2F%2Fraw.githubusercontent.com%2Fstatus102%2FTradeRecorder%2Fmaster%2FTradeBuddy%2FTradeRecorder.csproj)](https://github.com/status102/TradeRecorder/raw/master/latest.zip)
[![](https://img.shields.io/github/v/release/status102/TradeRecorder.svg)](https://github.com/status102/TradeRecorder/releases/latest)

记录交易的内容并根据预先设定计算交易金额，不会自动填写进交易窗口。

Record the content of the transaction and calculate the transaction amount according to the preset, and will not automatically fill in the transaction window.



## 使用方法

/tr 打开交易历史记录窗口，查看过往交易

![](https://github.com/status102/TradeRecorder/raw/master/Image/History.png)

/tr cfg 打开设置窗口设置物品的预期价格

![](https://github.com/status102/TradeRecorder/raw/master/Image/Setting.png)

## Todo

~~雇员出售列表颜色标注区分是否低于预期价格~~
~~市场板子标注预期的价格~~
- [x] 交易时即时查询交易物品价格
- [x] 对同一目标多次交易时，累积记录多次交易内容
- [x] 交易历史增加物品图标显示
~~交易数量支持多方案复合匹配~~
- [x] 更换为通过opcode或者sig获取交易itemId
- [x] 点击输出信息的角色名查看该角色交易记录
- [ ] 右键查看该角色交易记录

## Thanks

感谢獭爹、Loskh、zaevi、Yui以及幻想科技群群友

## 附

第一个稍微正经一点的项目，试着学习使用Git、版本控制、各种规范以及新玩意，依旧会很丑陋就是了
