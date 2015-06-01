<?xml version="1.0" encoding="ASCII"?>
<application:Application xmi:version="2.0" xmlns:xmi="http://www.omg.org/XMI" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:advanced="http://www.eclipse.org/ui/2010/UIModel/application/ui/advanced" xmlns:application="http://www.eclipse.org/ui/2010/UIModel/application" xmlns:basic="http://www.eclipse.org/ui/2010/UIModel/application/ui/basic" xmlns:menu="http://www.eclipse.org/ui/2010/UIModel/application/ui/menu" xmi:id="_OQJ8kAgjEeWbevYbb273yg" elementId="org.eclipse.e4.legacy.ide.application" contributorURI="platform:/plugin/org.eclipse.ui.workbench" selectedElement="_OQJ8kQgjEeWbevYbb273yg" bindingContexts="_OQMY7wgjEeWbevYbb273yg">
  <persistedState key="memento" value="&lt;?xml version=&quot;1.0&quot; encoding=&quot;UTF-8&quot;?>&#xD;&#xA;&lt;workbench>&#xD;&#xA;&lt;mruList>&#xD;&#xA;&lt;file factoryID=&quot;org.eclipse.ui.part.FileEditorInputFactory&quot; id=&quot;org.jboss.tools.common.model.ui.editor.EditorPartWrapper&quot; name=&quot;web.xml&quot; tooltip=&quot;Userbase/src/main/webapp/WEB-INF/web.xml&quot;>&#xD;&#xA;&lt;persistable path=&quot;/Userbase/src/main/webapp/WEB-INF/web.xml&quot;/>&#xD;&#xA;&lt;/file>&#xD;&#xA;&lt;file factoryID=&quot;org.eclipse.ui.part.FileEditorInputFactory&quot; id=&quot;com.springsource.sts.config.ui.editors.SpringConfigEditor&quot; name=&quot;root-context.xml&quot; tooltip=&quot;Userbase/src/main/webapp/WEB-INF/spring/root-context.xml&quot;>&#xD;&#xA;&lt;persistable path=&quot;/Userbase/src/main/webapp/WEB-INF/spring/root-context.xml&quot;/>&#xD;&#xA;&lt;/file>&#xD;&#xA;&lt;file factoryID=&quot;org.eclipse.ui.part.FileEditorInputFactory&quot; id=&quot;com.springsource.sts.config.ui.editors.SpringConfigEditor&quot; name=&quot;servlet-context.xml&quot; tooltip=&quot;Userbase/src/main/webapp/WEB-INF/spring/appServlet/servlet-context.xml&quot;>&#xD;&#xA;&lt;persistable path=&quot;/Userbase/src/main/webapp/WEB-INF/spring/appServlet/servlet-context.xml&quot;/>&#xD;&#xA;&lt;/file>&#xD;&#xA;&lt;file factoryID=&quot;org.eclipse.ui.part.FileEditorInputFactory&quot; id=&quot;org.eclipse.jdt.ui.CompilationUnitEditor&quot; name=&quot;UserDaoImpl.java&quot; tooltip=&quot;Userbase/src/main/java/com/divino/userbase/repository/UserDaoImpl.java&quot;>&#xD;&#xA;&lt;persistable path=&quot;/Userbase/src/main/java/com/divino/userbase/repository/UserDaoImpl.java&quot;/>&#xD;&#xA;&lt;/file>&#xD;&#xA;&lt;file factoryID=&quot;org.eclipse.ui.part.FileEditorInputFactory&quot; id=&quot;org.eclipse.jdt.ui.CompilationUnitEditor&quot; name=&quot;UserServiceImpl.java&quot; tooltip=&quot;Userbase/src/main/java/com/divino/userbase/service/UserServiceImpl.java&quot;>&#xD;&#xA;&lt;persistable path=&quot;/Userbase/src/main/java/com/divino/userbase/service/UserServiceImpl.java&quot;/>&#xD;&#xA;&lt;/file>&#xD;&#xA;&lt;file factoryID=&quot;org.eclipse.ui.part.FileEditorInputFactory&quot; id=&quot;org.eclipse.m2e.editor.MavenPomEditor&quot; name=&quot;pom.xml&quot; tooltip=&quot;Userbase/pom.xml&quot;>&#xD;&#xA;&lt;persistable path=&quot;/Userbase/pom.xml&quot;/>&#xD;&#xA;&lt;/file>&#xD;&#xA;&lt;file factoryID=&quot;org.eclipse.ui.part.FileEditorInputFactory&quot; id=&quot;org.eclipse.jdt.ui.CompilationUnitEditor&quot; name=&quot;UserController.java&quot; tooltip=&quot;Userbase/src/main/java/com/divino/userbase/controller/UserController.java&quot;>&#xD;&#xA;&lt;persistable path=&quot;/Userbase/src/main/java/com/divino/userbase/controller/UserController.java&quot;/>&#xD;&#xA;&lt;/file>&#xD;&#xA;&lt;file factoryID=&quot;org.eclipse.ui.part.FileEditorInputFactory&quot; id=&quot;org.eclipse.jdt.ui.CompilationUnitEditor&quot; name=&quot;UserService.java&quot; tooltip=&quot;Userbase/src/main/java/com/divino/userbase/service/UserService.java&quot;>&#xD;&#xA;&lt;persistable path=&quot;/Userbase/src/main/java/com/divino/userbase/service/UserService.java&quot;/>&#xD;&#xA;&lt;/file>&#xD;&#xA;&lt;file factoryID=&quot;org.eclipse.ui.part.FileEditorInputFactory&quot; id=&quot;org.eclipse.jdt.ui.CompilationUnitEditor&quot; name=&quot;User.java&quot; tooltip=&quot;Userbase/src/main/j           OrderByClause.OrderByDescending(selecotr);
            return OrderByClause;
        }

        public int? Limit { get; set; }
    }

    public class OrderBySelector<TEntity>
    {
        public OrderBySelector(object selector, Type keyType, Sort sort = Sort.Asc)
        {
            Selector = selector;
            KeyType = keyType;
            Sort = sort;
        }
        public object Selector { get; set; }
        public Type KeyType { get; set; }
        public Sort Sort { get; set; }
    }

    public class OrderByClause<TEntity>
    {
        public OrderByClause() { }

        private List<OrderBySelector<TEntity>> _orderBySelectors = new List<OrderBySelector<TEntity>>();

        public List<OrderBySelector<TEntity>> OrderBySelectors
        {
            get { return _orderBySelectors; }
            set { _orderBySelectors = value; }
        }

        public OrderByClause<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> orderBySelector)
        {
            Type keyType = typeof(TKey);

            OrderBySelectors.Add(new OrderBySelector<TEntity>(orderBySelector, keyType, Sort.Asc));

            return this;
        }

        public OrderByClause<TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> orderBySelector)
        {
            Type keyType = typeof(TKey);

            OrderBySelectors.Add(new OrderBySelector<TEntity>(orderBySelector, keyType, Sort.Desc));

            return this;
        }

        public OrderByClause<TEntity> ThenBy<TKey>(Expression<Func<TEntity, TKey>> orderBySelector)
        {
            Type keyType = typeof(TKey);

            OrderBySelectors.Add(new OrderBySelector<TEntity>(orderBySelector, keyType, Sort.Asc));

            return this;
        }

        public OrderByClause<TEntity> ThenByDescending<TKey>(Expression<Func<TEntity, TKey>> orderBySelector)
        {
            Type keyType = typeof(TKey);

            OrderBySelectors.Add(new OrderBySelector<TEntity>(orderBySelector, keyType, Sort.Desc));

            return this;
        }

    }

    public enum Sort
    {
        Asc,
        Desc
    }
}
