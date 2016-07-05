# THUStudentInfo
Grab THU student information from the graduation online processing system.

清华的离校系统中有一个权限问题，导致学生身份登录可以任意查看指定学号的某些信息和毕业照片。

具体地说，学生可以从Info（信息门户）登录后，漫游到bylx.cic...，按照设计，查看自己的毕业手续办理情况。对应的页面名为lxsxblxs.jsp。
但是，无意中将网址改为lxsxbl.jsp，发现可以输入任意学号，并查看对应信息。这可能是这个页面没有对登录角色进行验证导致的。
而从lxsxbl_history.jsp?xh=xxx可以看到往届毕业生的信息。

此外，从lxstulist.jsp可以看到本年度毕业的学生列表，因此不必要枚举学号。

不确定这样的操作是否会在后台留下异常记录，所以在抓取过程中，增加了每两个操作之间延时的设计。

在离校办理开放期间，可以使用此程序。从Info登录，选择“网上退宿舍”或“个人信息查询”，将此时名为JSESSIONID的cookies输入程序。
选择开始和结束的学号，以及应届或往届；或输入0则自动获取本年毕业生列表。

此程序仅供理论研究，由此带来的一切后果由使用者承担全部责任。
