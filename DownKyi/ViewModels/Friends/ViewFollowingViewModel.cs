﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DownKyi.Core.BiliApi.Users;
using DownKyi.Core.BiliApi.Users.Models;
using DownKyi.Core.Settings;
using DownKyi.Core.Settings.Models;
using DownKyi.Core.Storage;
using DownKyi.CustomControl;
using DownKyi.Utils;
using DownKyi.ViewModels.PageViewModels;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace DownKyi.ViewModels.Friends;

public class ViewFollowingViewModel : ViewModelBase
{
    public const string Tag = "PageFriendsFollowing";

    // mid
    private long mid = -1;

    // 每页数量，暂时在此写死，以后在设置中增加选项
    private readonly int NumberInPage = 20;

    #region 页面属性申明

    private string pageName = ViewFriendsViewModel.Tag;

    public string PageName
    {
        get => pageName;
        set => SetProperty(ref pageName, value);
    }

    private bool contentVisibility;

    public bool ContentVisibility
    {
        get => contentVisibility;
        set => SetProperty(ref contentVisibility, value);
    }

    private bool innerContentVisibility;

    public bool InnerContentVisibility
    {
        get => innerContentVisibility;
        set => SetProperty(ref innerContentVisibility, value);
    }

    private bool loading;

    public bool Loading
    {
        get => loading;
        set => SetProperty(ref loading, value);
    }

    private bool loadingVisibility;

    public bool LoadingVisibility
    {
        get => loadingVisibility;
        set => SetProperty(ref loadingVisibility, value);
    }

    private bool noDataVisibility;

    public bool NoDataVisibility
    {
        get => noDataVisibility;
        set => SetProperty(ref noDataVisibility, value);
    }

    private bool contentLoading;

    public bool ContentLoading
    {
        get => contentLoading;
        set => SetProperty(ref contentLoading, value);
    }

    private bool contentLoadingVisibility;

    public bool ContentLoadingVisibility
    {
        get => contentLoadingVisibility;
        set => SetProperty(ref contentLoadingVisibility, value);
    }

    private bool contentNoDataVisibility;

    public bool ContentNoDataVisibility
    {
        get => contentNoDataVisibility;
        set => SetProperty(ref contentNoDataVisibility, value);
    }

    private ObservableCollection<TabHeader> tabHeaders;

    public ObservableCollection<TabHeader> TabHeaders
    {
        get => tabHeaders;
        set => SetProperty(ref tabHeaders, value);
    }

    private int selectTabId;

    public int SelectTabId
    {
        get => selectTabId;
        set => SetProperty(ref selectTabId, value);
    }

    private bool isEnabled = true;

    public bool IsEnabled
    {
        get => isEnabled;
        set => SetProperty(ref isEnabled, value);
    }

    private CustomPagerViewModel pager;

    public CustomPagerViewModel Pager
    {
        get => pager;
        set => SetProperty(ref pager, value);
    }

    private ObservableCollection<FriendInfo> contents;

    public ObservableCollection<FriendInfo> Contents
    {
        get => contents;
        set => SetProperty(ref contents, value);
    }

    #endregion

    public ViewFollowingViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
    {
        #region 属性初始化

        // 初始化loading gif
        Loading = true;
        LoadingVisibility = false;
        NoDataVisibility = false;

        ContentLoading = true;
        ContentLoadingVisibility = false;
        ContentNoDataVisibility = false;

        TabHeaders = new ObservableCollection<TabHeader>();
        Contents = new ObservableCollection<FriendInfo>();

        #endregion
    }

    #region 命令申明

    // 左侧tab点击事件
    private DelegateCommand<object> leftTabHeadersCommand;

    public DelegateCommand<object> LeftTabHeadersCommand => leftTabHeadersCommand ?? (leftTabHeadersCommand =
        new DelegateCommand<object>(ExecuteLeftTabHeadersCommand, CanExecuteLeftTabHeadersCommand));

    /// <summary>
    /// 左侧tab点击事件
    /// </summary>
    /// <param name="parameter"></param>
    private void ExecuteLeftTabHeadersCommand(object parameter)
    {
        if (!(parameter is TabHeader tabHeader))
        {
            return;
        }

        // 页面选择
        Pager = new CustomPagerViewModel(1, (int)Math.Ceiling(double.Parse(tabHeader.SubTitle) / NumberInPage));
        Pager.CurrentChanged += OnCurrentChanged_Pager;
        Pager.CountChanged += OnCountChanged_Pager;
        Pager.Current = 1;
    }

    /// <summary>
    /// 左侧tab点击事件是否允许执行
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns></returns>
    private bool CanExecuteLeftTabHeadersCommand(object parameter)
    {
        return IsEnabled;
    }

    #endregion

    /// <summary>
    /// 初始化页面数据
    /// </summary>
    private void InitView()
    {
        ContentVisibility = false;
        InnerContentVisibility = false;
        LoadingVisibility = true;
        NoDataVisibility = false;
        ContentLoadingVisibility = false;
        ContentNoDataVisibility = false;

        TabHeaders.Clear();
        Contents.Clear();
        SelectTabId = -1;
    }

    /// <summary>
    /// 初始化左侧列表
    /// </summary>
    private async void InitLeftTable()
    {
        TabHeaders.Clear();

        UserInfoSettings userInfo = SettingsManager.GetInstance().GetUserInfo();
        if (userInfo != null && userInfo.Mid == mid)
        {
            // 用户的关系状态数
            UserRelationStat relationStat = null;
            await Task.Run(() => { relationStat = UserStatus.GetUserRelationStat(mid); });
            if (relationStat != null)
            {
                TabHeaders.Add(new TabHeader
                {
                    Id = -1, Title = DictionaryResource.GetString("AllFollowing"),
                    SubTitle = relationStat.Following.ToString()
                });
                TabHeaders.Add(new TabHeader
                {
                    Id = -2, Title = DictionaryResource.GetString("WhisperFollowing"),
                    SubTitle = relationStat.Whisper.ToString()
                });
            }

            // 用户的关注分组
            List<FollowingGroup> followingGroup = null;
            await Task.Run(() => { followingGroup = UserRelation.GetFollowingGroup(); });
            if (followingGroup != null)
            {
                foreach (FollowingGroup tag in followingGroup)
                {
                    TabHeaders.Add(new TabHeader { Id = tag.TagId, Title = tag.Name, SubTitle = tag.Count.ToString() });
                }
            }
        }
        else
        {
            // 用户的关系状态数
            UserRelationStat relationStat = null;
            await Task.Run(() => { relationStat = UserStatus.GetUserRelationStat(mid); });
            if (relationStat != null)
            {
                TabHeaders.Add(new TabHeader
                {
                    Id = -1, Title = DictionaryResource.GetString("AllFollowing"),
                    SubTitle = relationStat.Following.ToString()
                });
            }
        }

        ContentVisibility = true;
        LoadingVisibility = false;
    }

    private void LoadContent(List<RelationFollowInfo> contents)
    {
        InnerContentVisibility = true;
        ContentLoadingVisibility = false;
        ContentNoDataVisibility = false;
        foreach (var item in contents)
        {
            StorageHeader storageHeader = new StorageHeader();
            Bitmap header = storageHeader.GetHeaderThumbnail(item.Mid, item.Name, item.Face, 64, 64);
            App.PropertyChangeAsync(new Action(() =>
            {
                Contents.Add(new FriendInfo(EventAggregator)
                    { Mid = item.Mid, Header = header, Name = item.Name, Sign = item.Sign });
            }));
        }
    }

    private async Task<bool> LoadAllFollowings(int pn, int ps)
    {
        List<RelationFollowInfo> contents = null;
        await Task.Run(() =>
        {
            RelationFollow data = UserRelation.GetFollowings(mid, pn, ps);
            if (data != null && data.List != null && data.List.Count > 0)
            {
                contents = data.List;
            }

            if (contents == null)
            {
                return;
            }

            LoadContent(contents);
        });

        if (contents == null)
        {
            return false;
        }

        return true;
    }

    private async Task<bool> LoadWhispers(int pn, int ps)
    {
        List<RelationFollowInfo> contents = null;
        await Task.Run(() =>
        {
            contents = UserRelation.GetWhispers(pn, ps);
            if (contents == null)
            {
                return;
            }

            LoadContent(contents);
        });

        if (contents == null)
        {
            return false;
        }

        return true;
    }

    private async Task<bool> LoadFollowingGroupContent(long tagId, int pn, int ps)
    {
        List<RelationFollowInfo> contents = null;
        await Task.Run(() =>
        {
            contents = UserRelation.GetFollowingGroupContent(tagId, pn, ps);
            if (contents == null)
            {
                return;
            }

            LoadContent(contents);
        });

        if (contents == null)
        {
            return false;
        }

        return true;
    }

    private async void UpdateContent(int current)
    {
        // 是否正在获取数据
        // 在所有的退出分支中都需要设为true
        IsEnabled = false;

        Contents.Clear();
        InnerContentVisibility = false;
        ContentLoadingVisibility = true;
        ContentNoDataVisibility = false;

        TabHeader tab = TabHeaders[SelectTabId];

        bool isSucceed;
        switch (tab.Id)
        {
            case -1:
                isSucceed = await LoadAllFollowings(current, NumberInPage);
                break;
            case -2:
                isSucceed = await LoadWhispers(current, NumberInPage);
                break;
            default:
                isSucceed = await LoadFollowingGroupContent(tab.Id, current, NumberInPage);
                break;
        }

        if (isSucceed)
        {
            InnerContentVisibility = true;
            ContentLoadingVisibility = false;
            ContentNoDataVisibility = false;
        }
        else
        {
            InnerContentVisibility = false;
            ContentLoadingVisibility = false;
            ContentNoDataVisibility = true;
        }

        IsEnabled = true;
    }

    private void OnCountChanged_Pager(int count)
    {
    }

    private bool OnCurrentChanged_Pager(int old, int current)
    {
        if (!IsEnabled)
        {
            //Pager.Current = old;
            return false;
        }

        UpdateContent(current);

        return true;
    }

    /// <summary>
    /// 导航到页面时执行
    /// </summary>
    /// <param name="navigationContext"></param>
    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        // 传入mid
        long parameter = navigationContext.Parameters.GetValue<long>("mid");
        if (parameter == 0)
        {
            return;
        }

        mid = parameter;

        // 是否是从PageFriends的headerTable的item点击进入的
        // true表示加载PageFriends后第一次进入此页面
        // false表示从headerTable的item点击进入的
        bool isFirst = navigationContext.Parameters.GetValue<bool>("isFirst");
        if (isFirst)
        {
            InitView();

            // 初始化左侧列表
            InitLeftTable();

            // 进入页面时显示的设置项
            SelectTabId = 0;
        }
    }
}